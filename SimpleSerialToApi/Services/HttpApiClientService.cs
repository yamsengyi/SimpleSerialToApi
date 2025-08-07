using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// HTTP-based implementation of API client service
    /// </summary>
    public class HttpApiClientService : IApiClientService
    {
        private readonly IApiClientFactory _clientFactory;
        private readonly IConfigurationService _configService;
        private readonly ILogger<HttpApiClientService> _logger;
        private readonly Dictionary<string, IApiAuthenticator> _authenticators;

        public HttpApiClientService(
            IApiClientFactory clientFactory,
            IConfigurationService configService,
            ILogger<HttpApiClientService> logger,
            IEnumerable<IApiAuthenticator> authenticators)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _authenticators = new Dictionary<string, IApiAuthenticator>();
            foreach (var auth in authenticators ?? Enumerable.Empty<IApiAuthenticator>())
            {
                _authenticators[auth.AuthenticationType.ToString()] = auth;
            }
        }

        public async Task<ApiResponse> PostAsync<T>(string endpointName, T data)
        {
            return await SendRequestAsync(endpointName, HttpMethod.Post, data);
        }

        public async Task<ApiResponse> PutAsync<T>(string endpointName, T data)
        {
            return await SendRequestAsync(endpointName, HttpMethod.Put, data);
        }

        public async Task<ApiResponse> PatchAsync<T>(string endpointName, T data)
        {
            return await SendRequestAsync(endpointName, HttpMethod.Patch, data);
        }

        public async Task<ApiResponse> GetAsync(string endpointName, Dictionary<string, string>? queryParams = null)
        {
            return await SendRequestAsync<object>(endpointName, HttpMethod.Get, null, queryParams);
        }

        public async Task<ApiResponse> DeleteAsync(string endpointName, string resourceId)
        {
            var queryParams = new Dictionary<string, string> { { "id", resourceId } };
            return await SendRequestAsync<object>(endpointName, HttpMethod.Delete, null, queryParams);
        }

        public async Task<BatchApiResponse> PostBatchAsync<T>(string endpointName, List<T> dataList)
        {
            var stopwatch = Stopwatch.StartNew();
            var batchResponse = new BatchApiResponse
            {
                TotalRequests = dataList.Count
            };

            _logger.LogInformation("Starting batch POST to endpoint {EndpointName} with {Count} items", 
                endpointName, dataList.Count);

            var tasks = dataList.Select(async data =>
            {
                try
                {
                    var response = await PostAsync(endpointName, data);
                    if (response.IsSuccess)
                        batchResponse.SuccessfulRequests++;
                    else
                        batchResponse.FailedRequests++;
                    
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in batch request for endpoint {EndpointName}", endpointName);
                    batchResponse.FailedRequests++;
                    return ApiResponse.Failure(0, ex.Message);
                }
            });

            batchResponse.Responses = (await Task.WhenAll(tasks)).ToList();
            batchResponse.TotalProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation("Completed batch POST to endpoint {EndpointName}: {Successful}/{Total} successful in {Duration}ms", 
                endpointName, batchResponse.SuccessfulRequests, batchResponse.TotalRequests, batchResponse.TotalProcessingTime.TotalMilliseconds);

            return batchResponse;
        }

        public async Task<HealthCheckResult> HealthCheckAsync(string endpointName)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Performing health check for endpoint {EndpointName}", endpointName);

                // For health check, we'll send a simple GET request
                var response = await GetAsync(endpointName);
                stopwatch.Stop();

                if (response.IsSuccess)
                {
                    return HealthCheckResult.Healthy(endpointName, stopwatch.Elapsed, response.StatusCode);
                }
                else
                {
                    return HealthCheckResult.Unhealthy(endpointName, response.ErrorMessage, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Health check failed for endpoint {EndpointName}", endpointName);
                return HealthCheckResult.Unhealthy(endpointName, ex.Message);
            }
        }

        private async Task<ApiResponse> SendRequestAsync<T>(
            string endpointName, 
            HttpMethod method, 
            T? data, 
            Dictionary<string, string>? queryParams = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var messageId = Guid.NewGuid().ToString();

            try
            {
                // Get endpoint configuration
                var endpointConfig = GetEndpointConfiguration(endpointName);
                if (endpointConfig == null)
                {
                    return ApiResponse.Failure(0, $"Endpoint configuration not found: {endpointName}", stopwatch.Elapsed, messageId);
                }

                // Get HTTP client
                var httpClient = _clientFactory.GetClient(endpointName);

                // Build request URL
                var url = BuildRequestUrl(endpointConfig.Url, queryParams);

                // Create HTTP request
                var request = new HttpRequestMessage(method, url);
                request.Headers.Add("X-Request-ID", messageId);

                // Add authentication
                await ApplyAuthenticationAsync(request, endpointConfig);

                // Add request body for data-carrying methods
                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    var jsonContent = JsonConvert.SerializeObject(data);
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    _logger.LogDebug("Sending {Method} request to {Url} with payload: {Payload}", 
                        method, url, jsonContent);
                }
                else
                {
                    _logger.LogDebug("Sending {Method} request to {Url}", method, url);
                }

                // Send request
                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                stopwatch.Stop();

                var apiResponse = new ApiResponse
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseContent = responseContent,
                    ResponseTime = stopwatch.Elapsed,
                    MessageId = messageId,
                    RequestUrl = url,
                    HttpMethod = method.Method
                };

                // Add response headers
                foreach (var header in response.Headers)
                {
                    apiResponse.ResponseHeaders[header.Key] = string.Join(", ", header.Value);
                }

                if (!response.IsSuccessStatusCode)
                {
                    apiResponse.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    _logger.LogWarning("API request failed: {Method} {Url} returned {StatusCode}: {Error}", 
                        method, url, response.StatusCode, apiResponse.ErrorMessage);
                }
                else
                {
                    _logger.LogDebug("API request succeeded: {Method} {Url} returned {StatusCode} in {Duration}ms", 
                        method, url, response.StatusCode, stopwatch.ElapsedMilliseconds);
                }

                return apiResponse;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "HTTP request exception for {Method} {EndpointName}", method, endpointName);
                return ApiResponse.Failure(0, $"HTTP request failed: {ex.Message}", stopwatch.Elapsed, messageId);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request timeout for {Method} {EndpointName}", method, endpointName);
                return ApiResponse.Failure(0, "Request timed out", stopwatch.Elapsed, messageId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected exception for {Method} {EndpointName}", method, endpointName);
                return ApiResponse.Failure(0, $"Unexpected error: {ex.Message}", stopwatch.Elapsed, messageId);
            }
        }

        private ApiEndpointConfig? GetEndpointConfiguration(string endpointName)
        {
            try
            {
                var config = _configService.ApplicationConfig;
                return config.ApiEndpoints.FirstOrDefault(e => e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get endpoint configuration for {EndpointName}", endpointName);
                return null;
            }
        }

        private static string BuildRequestUrl(string baseUrl, Dictionary<string, string>? queryParams)
        {
            if (queryParams == null || queryParams.Count == 0)
                return baseUrl;

            var queryString = string.Join("&", 
                queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var separator = baseUrl.Contains('?') ? "&" : "?";
            return $"{baseUrl}{separator}{queryString}";
        }

        private async Task ApplyAuthenticationAsync(HttpRequestMessage request, ApiEndpointConfig endpointConfig)
        {
            if (string.IsNullOrEmpty(endpointConfig.AuthType) || 
                endpointConfig.AuthType.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!_authenticators.TryGetValue(endpointConfig.AuthType, out var authenticator))
            {
                _logger.LogWarning("No authenticator found for auth type: {AuthType}", endpointConfig.AuthType);
                return;
            }

            try
            {
                var authResult = await authenticator.AuthenticateAsync(endpointConfig);
                if (authResult.IsSuccess)
                {
                    if (authResult.TokenType.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authResult.AccessToken);
                    }
                    else if (authResult.TokenType.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                    }
                    else if (authResult.TokenType.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.Add("X-API-Key", authResult.AccessToken);
                    }
                    else
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authResult.TokenType, authResult.AccessToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Authentication failed for endpoint {EndpointName}: {Error}", 
                        endpointConfig.Name, authResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during authentication for endpoint {EndpointName}", endpointConfig.Name);
            }
        }
    }
}