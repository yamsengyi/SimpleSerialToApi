using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services.Authentication
{
    /// <summary>
    /// Base class for API authenticators
    /// </summary>
    public abstract class BaseApiAuthenticator : IApiAuthenticator
    {
        protected readonly HttpClient _httpClient;
        protected readonly ILogger _logger;
        protected readonly Dictionary<string, AuthenticationResult> _authenticationCache;

        public abstract AuthenticationType AuthenticationType { get; }

        protected BaseApiAuthenticator(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authenticationCache = new Dictionary<string, AuthenticationResult>();
        }

        public virtual async Task<AuthenticationResult> AuthenticateAsync(ApiEndpointConfig endpoint)
        {
            if (endpoint == null)
            {
                return AuthenticationResult.Failure("Endpoint configuration is null");
            }

            var cacheKey = GetCacheKey(endpoint.Name);
            
            // Check if we have a valid cached authentication
            if (_authenticationCache.TryGetValue(cacheKey, out var cachedResult) && 
                cachedResult.IsSuccess && !cachedResult.IsExpired)
            {
                _logger.LogDebug("Using cached authentication for endpoint {EndpointName}", endpoint.Name);
                return cachedResult;
            }

            try
            {
                _logger.LogInformation("Authenticating against endpoint {EndpointName} using {AuthType}", 
                    endpoint.Name, AuthenticationType);

                var result = await PerformAuthenticationAsync(endpoint);
                
                if (result.IsSuccess)
                {
                    _authenticationCache[cacheKey] = result;
                    _logger.LogInformation("Successfully authenticated against endpoint {EndpointName}", endpoint.Name);
                }
                else
                {
                    _logger.LogWarning("Authentication failed for endpoint {EndpointName}: {Error}", 
                        endpoint.Name, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during authentication for endpoint {EndpointName}", endpoint.Name);
                return AuthenticationResult.Failure($"Authentication exception: {ex.Message}");
            }
        }

        public virtual async Task<bool> RefreshTokenAsync(ApiEndpointConfig endpoint)
        {
            var cacheKey = GetCacheKey(endpoint.Name);
            
            if (!_authenticationCache.TryGetValue(cacheKey, out var currentAuth) || 
                string.IsNullOrEmpty(currentAuth.RefreshToken))
            {
                _logger.LogWarning("No refresh token available for endpoint {EndpointName}", endpoint.Name);
                return false;
            }

            try
            {
                _logger.LogInformation("Refreshing token for endpoint {EndpointName}", endpoint.Name);
                
                var refreshResult = await PerformTokenRefreshAsync(endpoint, currentAuth.RefreshToken);
                
                if (refreshResult.IsSuccess)
                {
                    _authenticationCache[cacheKey] = refreshResult;
                    _logger.LogInformation("Successfully refreshed token for endpoint {EndpointName}", endpoint.Name);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Token refresh failed for endpoint {EndpointName}: {Error}", 
                        endpoint.Name, refreshResult.ErrorMessage);
                    
                    // Clear the failed authentication
                    ClearAuthentication(endpoint.Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during token refresh for endpoint {EndpointName}", endpoint.Name);
                ClearAuthentication(endpoint.Name);
                return false;
            }
        }

        public virtual void ClearAuthentication(string endpointName)
        {
            var cacheKey = GetCacheKey(endpointName);
            if (_authenticationCache.Remove(cacheKey))
            {
                _logger.LogInformation("Cleared authentication cache for endpoint {EndpointName}", endpointName);
            }
        }

        public virtual AuthenticationResult? GetAuthenticationStatus(string endpointName)
        {
            var cacheKey = GetCacheKey(endpointName);
            return _authenticationCache.TryGetValue(cacheKey, out var result) ? result : null;
        }

        protected abstract Task<AuthenticationResult> PerformAuthenticationAsync(ApiEndpointConfig endpoint);

        protected virtual Task<AuthenticationResult> PerformTokenRefreshAsync(ApiEndpointConfig endpoint, string refreshToken)
        {
            // Default implementation - many auth types don't support refresh
            return Task.FromResult(AuthenticationResult.Failure("Token refresh not supported"));
        }

        protected virtual string GetCacheKey(string endpointName)
        {
            return $"{AuthenticationType}:{endpointName}";
        }

        protected virtual void ApplyAuthenticationToRequest(HttpRequestMessage request, AuthenticationResult authentication)
        {
            if (authentication.IsSuccess && !string.IsNullOrEmpty(authentication.AccessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    authentication.TokenType, authentication.AccessToken);
            }
        }
    }

    /// <summary>
    /// No authentication implementation
    /// </summary>
    public class NoneAuthenticator : BaseApiAuthenticator
    {
        public override AuthenticationType AuthenticationType => AuthenticationType.None;

        public NoneAuthenticator(HttpClient httpClient, ILogger<NoneAuthenticator> logger) 
            : base(httpClient, logger)
        {
        }

        protected override Task<AuthenticationResult> PerformAuthenticationAsync(ApiEndpointConfig endpoint)
        {
            return Task.FromResult(AuthenticationResult.Success("none", "None"));
        }
    }

    /// <summary>
    /// Bearer token authentication implementation
    /// </summary>
    public class BearerTokenAuthenticator : BaseApiAuthenticator
    {
        public override AuthenticationType AuthenticationType => AuthenticationType.BearerToken;

        public BearerTokenAuthenticator(HttpClient httpClient, ILogger<BearerTokenAuthenticator> logger) 
            : base(httpClient, logger)
        {
        }

        protected override Task<AuthenticationResult> PerformAuthenticationAsync(ApiEndpointConfig endpoint)
        {
            if (string.IsNullOrEmpty(endpoint.AuthToken))
            {
                return Task.FromResult(AuthenticationResult.Failure("Bearer token is required but not provided"));
            }

            var result = AuthenticationResult.Success(endpoint.AuthToken, "Bearer", DateTime.UtcNow.AddDays(1));
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Basic authentication implementation
    /// </summary>
    public class BasicAuthAuthenticator : BaseApiAuthenticator
    {
        public override AuthenticationType AuthenticationType => AuthenticationType.BasicAuth;

        public BasicAuthAuthenticator(HttpClient httpClient, ILogger<BasicAuthAuthenticator> logger) 
            : base(httpClient, logger)
        {
        }

        protected override Task<AuthenticationResult> PerformAuthenticationAsync(ApiEndpointConfig endpoint)
        {
            if (string.IsNullOrEmpty(endpoint.AuthToken))
            {
                return Task.FromResult(AuthenticationResult.Failure("Basic auth credentials are required but not provided"));
            }

            // AuthToken should be in format "username:password" or base64 encoded credentials
            var credentials = endpoint.AuthToken;
            if (!credentials.Contains(':') && IsBase64String(credentials))
            {
                // Already base64 encoded
                var result = AuthenticationResult.Success(credentials, "Basic", DateTime.MaxValue);
                return Task.FromResult(result);
            }
            else if (credentials.Contains(':'))
            {
                // Needs to be encoded
                var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
                var result = AuthenticationResult.Success(encoded, "Basic", DateTime.MaxValue);
                return Task.FromResult(result);
            }
            else
            {
                return Task.FromResult(AuthenticationResult.Failure("Invalid basic auth credentials format"));
            }
        }

        private static bool IsBase64String(string s)
        {
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// API Key authentication implementation
    /// </summary>
    public class ApiKeyAuthenticator : BaseApiAuthenticator
    {
        public override AuthenticationType AuthenticationType => AuthenticationType.ApiKey;

        public ApiKeyAuthenticator(HttpClient httpClient, ILogger<ApiKeyAuthenticator> logger) 
            : base(httpClient, logger)
        {
        }

        protected override Task<AuthenticationResult> PerformAuthenticationAsync(ApiEndpointConfig endpoint)
        {
            if (string.IsNullOrEmpty(endpoint.AuthToken))
            {
                return Task.FromResult(AuthenticationResult.Failure("API key is required but not provided"));
            }

            var result = AuthenticationResult.Success(endpoint.AuthToken, "ApiKey", DateTime.MaxValue);
            return Task.FromResult(result);
        }

        protected override void ApplyAuthenticationToRequest(HttpRequestMessage request, AuthenticationResult authentication)
        {
            if (authentication.IsSuccess && !string.IsNullOrEmpty(authentication.AccessToken))
            {
                // API key can be applied in different ways - this is a common pattern
                request.Headers.Add("X-API-Key", authentication.AccessToken);
                
                // Some APIs also use Authorization header with ApiKey scheme
                // request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("ApiKey", authentication.AccessToken);
            }
        }
    }
}