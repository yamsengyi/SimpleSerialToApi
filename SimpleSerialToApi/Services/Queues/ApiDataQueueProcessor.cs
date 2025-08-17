using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Services.Queues
{
    /// <summary>
    /// Queue processor for API data messages
    /// </summary>
    public class ApiDataQueueProcessor : IQueueProcessor<MappedApiData>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ApiDataQueueProcessor> _logger;

        /// <summary>
        /// Maximum number of messages to process in a batch
        /// </summary>
        public int MaxBatchSize => 50;

        /// <summary>
        /// Name of the processor
        /// </summary>
        public string ProcessorName => "ApiDataProcessor";

        /// <summary>
        /// Whether this processor supports batch processing
        /// </summary>
        public bool SupportsBatchProcessing => true;

        /// <summary>
        /// Creates a new API data queue processor
        /// </summary>
        /// <param name="httpClient">HTTP client for API calls</param>
        /// <param name="configurationService">Configuration service</param>
        /// <param name="logger">Logger instance</param>
        public ApiDataQueueProcessor(HttpClient httpClient, IConfigurationService configurationService, ILogger<ApiDataQueueProcessor> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes a single message
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <returns>Processing result</returns>
        public async Task<ProcessingResult> ProcessAsync(QueueMessage<MappedApiData> message)
        {
            if (message?.Payload == null)
            {
                return ProcessingResult.CreateFailure("Message or payload is null", false);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var apiData = message.Payload;

                // Get API endpoint configuration
                var endpoint = _configurationService.ApiEndpoints.FirstOrDefault(e => e.Name == apiData.EndpointName);

                if (endpoint == null)
                {
                    return ProcessingResult.CreateFailure($"API endpoint '{apiData.EndpointName}' not found", false);
                }

                // Prepare HTTP request
                var requestMessage = new HttpRequestMessage();
                
                // 시나리오에서 설정한 HTTP 메서드 사용 (우선순위: 시나리오 > 엔드포인트 설정)
                var httpMethod = !string.IsNullOrEmpty(apiData.ApiMethod) ? apiData.ApiMethod : endpoint.Method;
                requestMessage.Method = new HttpMethod(httpMethod);
                
                // Full Path 구성: Base URL + API Endpoint
                var fullUrl = endpoint.Url.TrimEnd('/');
                if (!string.IsNullOrEmpty(apiData.ApiEndpoint))
                {
                    var apiEndpoint = apiData.ApiEndpoint.TrimStart('/');
                    fullUrl = $"{fullUrl}/{apiEndpoint}";
                }
                requestMessage.RequestUri = new Uri(fullUrl);

                // Add headers
                foreach (var header in endpoint.Headers)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Add authentication
                if (!string.IsNullOrEmpty(endpoint.AuthType) && !string.IsNullOrEmpty(endpoint.AuthToken))
                {
                    switch (endpoint.AuthType.ToLower())
                    {
                        case "bearer":
                            requestMessage.Headers.Authorization = 
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", endpoint.AuthToken);
                            break;
                        case "basic":
                            requestMessage.Headers.Authorization = 
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", endpoint.AuthToken);
                            break;
                    }
                }

                // GET, DELETE 등은 일반적으로 body가 없으므로 HTTP 메서드에 따라 처리
                if (httpMethod.ToUpper() == "GET" || httpMethod.ToUpper() == "DELETE")
                {
                    // GET/DELETE 요청은 body 없이 전송
                    _logger.LogInformation("Sending HTTP request {Method} FULL PATH: {FullUrl} (no body for {Method} request)", 
                        httpMethod, fullUrl, httpMethod);
                }
                else
                {
                    // POST, PUT, PATCH 등은 body와 함께 전송
                    // Payload 처리: Dictionary에서 실제 데이터 추출
                    string payloadContent;
                    string contentType = "application/json"; // 기본값
                    
                    // ContentType 설정 (시나리오에서 지정한 경우 사용)
                    if (!string.IsNullOrEmpty(apiData.ContentType))
                    {
                        contentType = apiData.ContentType;
                    }
                    
                    // Payload에서 실제 데이터 추출
                    if (apiData.Payload != null && apiData.Payload.ContainsKey("data"))
                    {
                        // "data" 키에서 실제 처리된 데이터 가져오기
                        var dataValue = apiData.Payload["data"];
                        if (dataValue is string stringData)
                        {
                            payloadContent = stringData;
                        }
                        else
                        {
                            // 문자열이 아닌 경우 JSON 직렬화
                            payloadContent = JsonSerializer.Serialize(dataValue, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                WriteIndented = false
                            });
                        }
                    }
                    else if (apiData.Payload != null && apiData.Payload.Count > 0)
                    {
                        // 전체 Payload를 JSON으로 직렬화
                        payloadContent = JsonSerializer.Serialize(apiData.Payload, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = false
                        });
                    }
                    else
                    {
                        payloadContent = string.Empty;
                    }

                    requestMessage.Content = new StringContent(payloadContent, Encoding.UTF8, contentType);

                    // 전송 직전 로깅 - FULL PATH와 실제 전송 데이터 확인
                    _logger.LogInformation("Sending HTTP request {Method} FULL PATH: {FullUrl} with payload: {Payload}, ContentType: {ContentType}", 
                        httpMethod, fullUrl, payloadContent, contentType);
                }

                // Set timeout
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(endpoint.Timeout));

                // Send request
                var response = await _httpClient.SendAsync(requestMessage, cts.Token);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    return ProcessingResult.CreateSuccess(stopwatch.Elapsed, new Dictionary<string, object>
                    {
                        { "StatusCode", (int)response.StatusCode },
                        { "ResponseContent", responseContent },
                        { "MessageId", message.MessageId },
                        { "EndpointName", apiData.EndpointName }
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var shouldRetry = IsRetryableStatusCode(response.StatusCode);
                    
                    return ProcessingResult.CreateFailure(
                        $"HTTP {(int)response.StatusCode}: {errorContent}",
                        shouldRetry,
                        stopwatch.Elapsed);
                }
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                return ProcessingResult.CreateFailure("Request timeout", true, stopwatch.Elapsed);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                return ProcessingResult.CreateFailure($"HTTP request error: {ex.Message}", true, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ProcessingResult.CreateFailure($"Unexpected error: {ex.Message}", false, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Processes a batch of messages
        /// </summary>
        /// <param name="messages">Messages to process</param>
        /// <returns>Batch processing result</returns>
        public async Task<BatchProcessingResult> ProcessBatchAsync(List<QueueMessage<MappedApiData>> messages)
        {
            if (messages == null || !messages.Any())
            {
                return new BatchProcessingResult();
            }

            var result = new BatchProcessingResult();
            var stopwatch = Stopwatch.StartNew();

            // Group messages by endpoint for batch processing
            var groupedMessages = messages.GroupBy(m => m.Payload?.EndpointName ?? "unknown");

            foreach (var group in groupedMessages)
            {
                foreach (var message in group)
                {
                    try
                    {
                        var processingResult = await ProcessAsync(message);
                        result.Results.Add(processingResult);

                        if (processingResult.Success)
                        {
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.FailedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Results.Add(ProcessingResult.CreateFailure($"Batch processing error: {ex.Message}", false));
                        result.FailedCount++;
                    }
                }
            }

            stopwatch.Stop();
            result.TotalProcessingTime = stopwatch.Elapsed;

            return result;
        }

        /// <summary>
        /// Determines if this processor can handle the given message
        /// </summary>
        /// <param name="message">Message to check</param>
        /// <returns>True if processor can handle the message</returns>
        public bool CanProcess(QueueMessage<MappedApiData> message)
        {
            if (message?.Payload == null)
                return false;

            // Check if endpoint name is valid
            var endpointName = message.Payload.EndpointName;
            if (string.IsNullOrEmpty(endpointName))
                return false;

            // Could add more validation here
            return true;
        }

        /// <summary>
        /// Determines if an HTTP status code is retryable
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>True if the request should be retried</returns>
        private static bool IsRetryableStatusCode(System.Net.HttpStatusCode statusCode)
        {
            return statusCode == System.Net.HttpStatusCode.InternalServerError ||
                   statusCode == System.Net.HttpStatusCode.BadGateway ||
                   statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                   statusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                   statusCode == System.Net.HttpStatusCode.TooManyRequests ||
                   statusCode == System.Net.HttpStatusCode.RequestTimeout;
        }
    }
}