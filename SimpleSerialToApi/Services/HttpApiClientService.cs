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
        private readonly ApiMonitorService _apiMonitorService;
        private readonly ApiFileLogService _apiFileLogService;
        private readonly Dictionary<string, IApiAuthenticator> _authenticators;

        public HttpApiClientService(
            IApiClientFactory clientFactory,
            IConfigurationService configService,
            ILogger<HttpApiClientService> logger,
            ApiMonitorService apiMonitorService,
            ApiFileLogService apiFileLogService,
            IEnumerable<IApiAuthenticator> authenticators)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiMonitorService = apiMonitorService ?? throw new ArgumentNullException(nameof(apiMonitorService));
            _apiFileLogService = apiFileLogService ?? throw new ArgumentNullException(nameof(apiFileLogService));

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

                // 연결이 되어 HTTP 응답을 받았다면 건강한 것으로 판단 (상태 코드에 관계없이)
                _logger.LogInformation("Health check for {EndpointName}: {StatusCode} {ResponseTime}ms", 
                    endpointName, response.StatusCode, stopwatch.ElapsedMilliseconds);
                
                return HealthCheckResult.Healthy(endpointName, stopwatch.Elapsed, response.StatusCode);
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
                string? requestBody = null;
                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    var jsonContent = JsonConvert.SerializeObject(data);
                    requestBody = jsonContent;
                    
                    // ContentType 설정 (기본값: application/json)
                    var contentType = endpointConfig.ContentType ?? "application/json";
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, contentType);
                    
                    _logger.LogInformation("Sending {Method} request to FULL PATH: {FullUrl} with payload: {Payload}, ContentType: {ContentType}, MessageId: {MessageId}", 
                        method, url, jsonContent, contentType, messageId);
                    
                    // 파일 로그: 요청 (with body)
                    await _apiFileLogService.LogRequestAsync(messageId, method.Method, url, requestBody, contentType);
                }
                else
                {
                    _logger.LogInformation("Sending {Method} request to FULL PATH: {FullUrl}, MessageId: {MessageId}", method, url, messageId);
                    
                    // 파일 로그: 요청 (without body)
                    await _apiFileLogService.LogRequestAsync(messageId, method.Method, url, null, "");
                }

                // Send request
                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                stopwatch.Stop();

                // 파일 로그: 응답
                await _apiFileLogService.LogResponseAsync(messageId, response.StatusCode, responseContent, stopwatch.Elapsed);

                // API 모니터에 Response 로깅
                _apiMonitorService.LogApiResponse(messageId, response.StatusCode, responseContent);

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
                
                // 파일 로그: 에러
                await _apiFileLogService.LogErrorAsync(messageId, ex);
                
                // API 모니터에 에러 로깅
                _apiMonitorService.LogApiError(messageId, ex);
                
                return ApiResponse.Failure(0, $"HTTP request failed: {ex.Message}", stopwatch.Elapsed, messageId);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request timeout for {Method} {EndpointName}", method, endpointName);
                
                // 파일 로그: 에러
                await _apiFileLogService.LogErrorAsync(messageId, ex);
                
                // API 모니터에 에러 로깅
                _apiMonitorService.LogApiError(messageId, ex);
                
                return ApiResponse.Failure(0, "Request timed out", stopwatch.Elapsed, messageId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected exception for {Method} {EndpointName}", method, endpointName);
                
                // 파일 로그: 에러
                await _apiFileLogService.LogErrorAsync(messageId, ex);
                
                // API 모니터에 에러 로깅
                _apiMonitorService.LogApiError(messageId, ex);
                
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