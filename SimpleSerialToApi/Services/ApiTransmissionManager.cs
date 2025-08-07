using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Manages API data transmission with retry logic and statistics
    /// </summary>
    public class ApiTransmissionManager : IApiTransmissionManager
    {
        private readonly IApiClientService _apiClient;
        private readonly IApiMonitor _apiMonitor;
        private readonly RetryPolicyFactory _retryPolicyFactory;
        private readonly ILogger<ApiTransmissionManager> _logger;
        private readonly List<FailedApiRequest> _failedRequests;
        private readonly object _lock = new object();

        public ApiTransmissionManager(
            IApiClientService apiClient,
            IApiMonitor apiMonitor,
            RetryPolicyFactory retryPolicyFactory,
            ILogger<ApiTransmissionManager> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _apiMonitor = apiMonitor ?? throw new ArgumentNullException(nameof(apiMonitor));
            _retryPolicyFactory = retryPolicyFactory ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _failedRequests = new List<FailedApiRequest>();
        }

        public async Task<ApiResponse> SendDataAsync(MappedApiData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _logger.LogDebug("Sending mapped data to endpoint {EndpointName} (MessageId: {MessageId})", 
                data.EndpointName, data.MessageId);

            try
            {
                // Create retry policy for this request
                var retryPolicy = _retryPolicyFactory.CreateDefaultRetryPolicy();
                
                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    var apiResponse = await _apiClient.PostAsync(data.EndpointName, data.Payload);
                    
                    // Record the API call for monitoring
                    _apiMonitor.RecordApiCall(data.EndpointName, data.Payload, apiResponse);
                    
                    if (!apiResponse.IsSuccess)
                    {
                        _logger.LogWarning("API call failed for endpoint {EndpointName} (MessageId: {MessageId}): {Error}", 
                            data.EndpointName, data.MessageId, apiResponse.ErrorMessage);
                    }
                    
                    return apiResponse;
                });

                if (response.IsSuccess)
                {
                    _logger.LogDebug("Successfully sent data to endpoint {EndpointName} (MessageId: {MessageId})", 
                        data.EndpointName, data.MessageId);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred sending data to endpoint {EndpointName} (MessageId: {MessageId})", 
                    data.EndpointName, data.MessageId);

                // Create failed request for potential retry later
                var failedRequest = new FailedApiRequest(data, data.EndpointName)
                {
                    LastErrorMessage = ex.Message,
                    AttemptCount = data.RetryCount + 1,
                    RetryPolicy = new RetryPolicy(),
                    NextRetryTime = DateTime.UtcNow.AddSeconds(30) // Default retry in 30 seconds
                };

                AddFailedRequest(failedRequest);

                return ApiResponse.Failure(0, ex.Message, messageId: data.MessageId);
            }
        }

        public async Task<BatchApiResponse> SendBatchAsync(List<MappedApiData> dataList)
        {
            if (dataList == null)
                throw new ArgumentNullException(nameof(dataList));

            _logger.LogInformation("Starting batch transmission of {Count} items", dataList.Count);

            // Group data by endpoint for efficient batch processing
            var endpointGroups = dataList.GroupBy(d => d.EndpointName).ToList();
            var allResponses = new List<ApiResponse>();

            foreach (var group in endpointGroups)
            {
                try
                {
                    _logger.LogDebug("Sending batch to endpoint {EndpointName} with {Count} items", 
                        group.Key, group.Count());

                    // Extract payload data for batch sending
                    var payloads = group.Select(d => d.Payload).ToList();
                    var batchResponse = await _apiClient.PostBatchAsync(group.Key, payloads);

                    // Record batch statistics
                    foreach (var response in batchResponse.Responses)
                    {
                        _apiMonitor.RecordApiCall(group.Key, new { BatchItem = true }, response);
                    }

                    allResponses.AddRange(batchResponse.Responses);

                    // Handle failed requests in the batch
                    var failedItems = group.Zip(batchResponse.Responses, (data, response) => new { Data = data, Response = response })
                        .Where(item => !item.Response.IsSuccess)
                        .ToList();

                    foreach (var failedItem in failedItems)
                    {
                        var failedRequest = new FailedApiRequest(failedItem.Data, group.Key)
                        {
                            LastErrorMessage = failedItem.Response.ErrorMessage,
                            LastStatusCode = failedItem.Response.StatusCode,
                            AttemptCount = failedItem.Data.RetryCount + 1,
                            RetryPolicy = new RetryPolicy(),
                            NextRetryTime = DateTime.UtcNow.AddSeconds(30)
                        };

                        AddFailedRequest(failedRequest);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during batch transmission to endpoint {EndpointName}", group.Key);

                    // Create failed responses for all items in this group
                    foreach (var data in group)
                    {
                        var failedResponse = ApiResponse.Failure(0, ex.Message, messageId: data.MessageId);
                        allResponses.Add(failedResponse);

                        var failedRequest = new FailedApiRequest(data, group.Key)
                        {
                            LastErrorMessage = ex.Message,
                            AttemptCount = data.RetryCount + 1,
                            RetryPolicy = new RetryPolicy(),
                            NextRetryTime = DateTime.UtcNow.AddSeconds(30)
                        };

                        AddFailedRequest(failedRequest);
                    }
                }
            }

            var finalBatchResponse = new BatchApiResponse
            {
                TotalRequests = dataList.Count,
                Responses = allResponses,
                SuccessfulRequests = allResponses.Count(r => r.IsSuccess),
                FailedRequests = allResponses.Count(r => !r.IsSuccess)
            };

            _logger.LogInformation("Completed batch transmission: {Successful}/{Total} successful", 
                finalBatchResponse.SuccessfulRequests, finalBatchResponse.TotalRequests);

            return finalBatchResponse;
        }

        public async Task<ApiResponse> RetryFailedRequestAsync(FailedApiRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!request.ShouldRetry)
            {
                _logger.LogWarning("Failed request {MessageId} should not be retried (attempts: {Attempts})", 
                    request.MessageId, request.AttemptCount);
                return ApiResponse.Failure(0, "Maximum retry attempts exceeded", messageId: request.MessageId);
            }

            _logger.LogInformation("Retrying failed request {MessageId} (attempt {Attempt})", 
                request.MessageId, request.AttemptCount + 1);

            try
            {
                // Update the original data retry count
                if (request.OriginalData != null)
                {
                    request.OriginalData.RetryCount = request.AttemptCount;
                }

                var response = await SendDataAsync(request.OriginalData!);

                if (response.IsSuccess)
                {
                    // Remove from failed requests list on success
                    RemoveFailedRequest(request);
                    _logger.LogInformation("Successfully retried failed request {MessageId}", request.MessageId);
                }
                else
                {
                    // Update failed request with new attempt info
                    request.AttemptCount++;
                    request.LastAttemptTime = DateTime.UtcNow;
                    request.LastErrorMessage = response.ErrorMessage;
                    request.LastStatusCode = response.StatusCode;
                    
                    if (request.RetryPolicy != null)
                    {
                        request.NextRetryTime = DateTime.UtcNow.Add(request.RetryPolicy.GetDelay(request.AttemptCount));
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during retry of failed request {MessageId}", request.MessageId);

                // Update failed request with exception info
                request.AttemptCount++;
                request.LastAttemptTime = DateTime.UtcNow;
                request.LastErrorMessage = ex.Message;
                
                if (request.RetryPolicy != null)
                {
                    request.NextRetryTime = DateTime.UtcNow.Add(request.RetryPolicy.GetDelay(request.AttemptCount));
                }

                return ApiResponse.Failure(0, ex.Message, messageId: request.MessageId);
            }
        }

        public async Task<HealthCheckResult> CheckEndpointHealthAsync(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            try
            {
                _logger.LogDebug("Performing health check for endpoint {EndpointName}", endpointName);
                
                var healthResult = await _apiClient.HealthCheckAsync(endpointName);
                
                // Also record this as a monitoring event
                var dummyResponse = new ApiResponse
                {
                    IsSuccess = healthResult.IsHealthy,
                    StatusCode = healthResult.StatusCode,
                    ResponseTime = healthResult.ResponseTime,
                    ErrorMessage = healthResult.IsHealthy ? "" : healthResult.Message
                };
                
                _apiMonitor.RecordApiCall(endpointName, "health-check", dummyResponse);

                return healthResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during health check for endpoint {EndpointName}", endpointName);
                return HealthCheckResult.Unhealthy(endpointName, ex.Message);
            }
        }

        public ApiStatistics GetEndpointStatistics(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            return _apiMonitor.GetStatistics(endpointName);
        }

        public List<FailedApiRequest> GetFailedRequests()
        {
            lock (_lock)
            {
                return _failedRequests.ToList();
            }
        }

        public void ClearFailedRequests(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            lock (_lock)
            {
                var toRemove = _failedRequests.Where(r => r.EndpointName.Equals(endpointName, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var request in toRemove)
                {
                    _failedRequests.Remove(request);
                }
                
                _logger.LogInformation("Cleared {Count} failed requests for endpoint {EndpointName}", toRemove.Count, endpointName);
            }
        }

        private void AddFailedRequest(FailedApiRequest failedRequest)
        {
            lock (_lock)
            {
                _failedRequests.Add(failedRequest);
                _logger.LogDebug("Added failed request {MessageId} to retry queue", failedRequest.MessageId);
            }
        }

        private void RemoveFailedRequest(FailedApiRequest failedRequest)
        {
            lock (_lock)
            {
                if (_failedRequests.Remove(failedRequest))
                {
                    _logger.LogDebug("Removed failed request {MessageId} from retry queue", failedRequest.MessageId);
                }
            }
        }
    }
}