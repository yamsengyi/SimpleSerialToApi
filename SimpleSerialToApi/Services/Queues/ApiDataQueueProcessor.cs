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

namespace SimpleSerialToApi.Services.Queues
{
    /// <summary>
    /// Queue processor for API data messages
    /// </summary>
    public class ApiDataQueueProcessor : IQueueProcessor<MappedApiData>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;

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
        public ApiDataQueueProcessor(HttpClient httpClient, IConfigurationService configurationService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
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
                requestMessage.Method = new HttpMethod(endpoint.Method);
                requestMessage.RequestUri = new Uri(endpoint.Url);

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

                // Serialize payload
                var jsonPayload = JsonSerializer.Serialize(apiData.Payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

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