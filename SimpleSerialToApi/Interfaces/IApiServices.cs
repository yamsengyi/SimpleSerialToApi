using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Interface for API authentication services
    /// </summary>
    public interface IApiAuthenticator
    {
        /// <summary>
        /// Authenticate against an API endpoint
        /// </summary>
        /// <param name="endpoint">API endpoint configuration</param>
        /// <returns>Authentication result</returns>
        Task<AuthenticationResult> AuthenticateAsync(ApiEndpointConfig endpoint);

        /// <summary>
        /// Refresh an existing authentication token
        /// </summary>
        /// <param name="endpoint">API endpoint configuration</param>
        /// <returns>Success if token was refreshed</returns>
        Task<bool> RefreshTokenAsync(ApiEndpointConfig endpoint);

        /// <summary>
        /// Clear authentication for an endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        void ClearAuthentication(string endpointName);

        /// <summary>
        /// Get current authentication status for an endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <returns>Authentication result or null if not authenticated</returns>
        AuthenticationResult? GetAuthenticationStatus(string endpointName);

        /// <summary>
        /// Authentication type supported by this authenticator
        /// </summary>
        AuthenticationType AuthenticationType { get; }
    }

    /// <summary>
    /// Interface for HTTP client operations
    /// </summary>
    public interface IApiClientService
    {
        /// <summary>
        /// Send a POST request with data
        /// </summary>
        /// <typeparam name="T">Type of data to send</typeparam>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <param name="data">Data to send</param>
        /// <returns>API response</returns>
        Task<ApiResponse> PostAsync<T>(string endpointName, T data);

        /// <summary>
        /// Send a PUT request with data
        /// </summary>
        /// <typeparam name="T">Type of data to send</typeparam>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <param name="data">Data to send</param>
        /// <returns>API response</returns>
        Task<ApiResponse> PutAsync<T>(string endpointName, T data);

        /// <summary>
        /// Send a GET request
        /// </summary>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>API response</returns>
        Task<ApiResponse> GetAsync(string endpointName, Dictionary<string, string>? queryParams = null);

        /// <summary>
        /// Send a batch POST request with multiple data items
        /// </summary>
        /// <typeparam name="T">Type of data to send</typeparam>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <param name="dataList">List of data items to send</param>
        /// <returns>Batch API response</returns>
        Task<BatchApiResponse> PostBatchAsync<T>(string endpointName, List<T> dataList);

        /// <summary>
        /// Check health of an API endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> HealthCheckAsync(string endpointName);

        /// <summary>
        /// Send a DELETE request
        /// </summary>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <param name="resourceId">ID of resource to delete</param>
        /// <returns>API response</returns>
        Task<ApiResponse> DeleteAsync(string endpointName, string resourceId);

        /// <summary>
        /// Send a PATCH request with data
        /// </summary>
        /// <typeparam name="T">Type of data to send</typeparam>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <param name="data">Data to send</param>
        /// <returns>API response</returns>
        Task<ApiResponse> PatchAsync<T>(string endpointName, T data);
    }

    /// <summary>
    /// Interface for managing API data transmission
    /// </summary>
    public interface IApiTransmissionManager
    {
        /// <summary>
        /// Send mapped API data to the configured endpoint
        /// </summary>
        /// <param name="data">Mapped API data to send</param>
        /// <returns>API response</returns>
        Task<ApiResponse> SendDataAsync(MappedApiData data);

        /// <summary>
        /// Send multiple mapped API data items as a batch
        /// </summary>
        /// <param name="dataList">List of mapped API data to send</param>
        /// <returns>Batch API response</returns>
        Task<BatchApiResponse> SendBatchAsync(List<MappedApiData> dataList);

        /// <summary>
        /// Retry a previously failed API request
        /// </summary>
        /// <param name="request">Failed request to retry</param>
        /// <returns>API response</returns>
        Task<ApiResponse> RetryFailedRequestAsync(FailedApiRequest request);

        /// <summary>
        /// Check health of a specific API endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint to check</param>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckEndpointHealthAsync(string endpointName);

        /// <summary>
        /// Get statistics for an API endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <returns>API statistics</returns>
        ApiStatistics GetEndpointStatistics(string endpointName);

        /// <summary>
        /// Get all failed requests that are eligible for retry
        /// </summary>
        /// <returns>List of failed requests</returns>
        List<FailedApiRequest> GetFailedRequests();

        /// <summary>
        /// Clear failed request history for an endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        void ClearFailedRequests(string endpointName);
    }

    /// <summary>
    /// Interface for retry policy implementations
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Execute an operation with retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <returns>Result of the operation</returns>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// Determine if a failed operation should be retried
        /// </summary>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="attemptNumber">Current attempt number</param>
        /// <returns>True if should retry</returns>
        bool ShouldRetry(Exception exception, int attemptNumber);

        /// <summary>
        /// Get the delay before the next retry attempt
        /// </summary>
        /// <param name="attemptNumber">Current attempt number</param>
        /// <returns>Delay duration</returns>
        TimeSpan GetDelay(int attemptNumber);

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        int MaxAttempts { get; }
    }

    /// <summary>
    /// Interface for API client factory
    /// </summary>
    public interface IApiClientFactory
    {
        /// <summary>
        /// Create an HTTP client for a specific endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <returns>Configured HTTP client</returns>
        HttpClient CreateClient(string endpointName);

        /// <summary>
        /// Get or create a cached HTTP client for an endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint configuration</param>
        /// <returns>Cached HTTP client</returns>
        HttpClient GetClient(string endpointName);

        /// <summary>
        /// Dispose and remove a cached HTTP client
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        void RemoveClient(string endpointName);

        /// <summary>
        /// Clear all cached HTTP clients
        /// </summary>
        void ClearAllClients();
    }

    /// <summary>
    /// Interface for API monitoring and metrics
    /// </summary>
    public interface IApiMonitor
    {
        /// <summary>
        /// Record an API request and response
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <param name="request">Request data</param>
        /// <param name="response">Response data</param>
        void RecordApiCall(string endpointName, object request, ApiResponse response);

        /// <summary>
        /// Get statistics for an API endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <returns>API statistics</returns>
        ApiStatistics GetStatistics(string endpointName);

        /// <summary>
        /// Get statistics for all endpoints
        /// </summary>
        /// <returns>Dictionary of endpoint statistics</returns>
        Dictionary<string, ApiStatistics> GetAllStatistics();

        /// <summary>
        /// Reset statistics for an endpoint
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        void ResetStatistics(string endpointName);

        /// <summary>
        /// Reset all statistics
        /// </summary>
        void ResetAllStatistics();

        /// <summary>
        /// Check if an endpoint is healthy based on recent statistics
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <returns>True if endpoint appears healthy</returns>
        bool IsEndpointHealthy(string endpointName);
    }
}