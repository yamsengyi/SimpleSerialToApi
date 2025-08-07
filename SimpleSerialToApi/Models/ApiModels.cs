using System;
using System.Collections.Generic;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// Types of authentication supported by the API client
    /// </summary>
    public enum AuthenticationType
    {
        None,
        BasicAuth,
        BearerToken,
        ApiKey,
        OAuth2,
        Custom
    }

    /// <summary>
    /// Configuration for HTTP client behavior
    /// </summary>
    public class HttpClientConfiguration
    {
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;
        public bool EnableCompression { get; set; } = true;
        public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
        public ProxyConfiguration? Proxy { get; set; }
        public SslConfiguration? Ssl { get; set; }
        public int MaxConcurrentRequests { get; set; } = 10;
        public bool UseExponentialBackoff { get; set; } = true;
    }

    /// <summary>
    /// Proxy configuration for HTTP client
    /// </summary>
    public class ProxyConfiguration
    {
        public string Address { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseDefaultCredentials { get; set; } = false;
    }

    /// <summary>
    /// SSL/TLS configuration for HTTP client
    /// </summary>
    public class SslConfiguration
    {
        public bool IgnoreCertificateErrors { get; set; } = false;
        public string ClientCertificatePath { get; set; } = string.Empty;
        public string ClientCertificatePassword { get; set; } = string.Empty;
        public List<string> TrustedCertificates { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of API authentication attempt
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public TimeSpan TimeToExpiry => ExpiresAt - DateTime.UtcNow;

        public static AuthenticationResult Success(string accessToken, string tokenType = "Bearer", DateTime expiresAt = default)
        {
            return new AuthenticationResult
            {
                IsSuccess = true,
                AccessToken = accessToken,
                TokenType = tokenType,
                ExpiresAt = expiresAt == default ? DateTime.UtcNow.AddHours(1) : expiresAt
            };
        }

        public static AuthenticationResult Failure(string errorMessage)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Response from an API call
    /// </summary>
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string ResponseContent { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ErrorMessage { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();
        public string RequestUrl { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;

        public static ApiResponse Success(int statusCode, string content, TimeSpan responseTime, string messageId = "")
        {
            return new ApiResponse
            {
                IsSuccess = true,
                StatusCode = statusCode,
                ResponseContent = content,
                ResponseTime = responseTime,
                MessageId = messageId
            };
        }

        public static ApiResponse Failure(int statusCode, string errorMessage, TimeSpan responseTime = default, string messageId = "")
        {
            return new ApiResponse
            {
                IsSuccess = false,
                StatusCode = statusCode,
                ErrorMessage = errorMessage,
                ResponseTime = responseTime,
                MessageId = messageId
            };
        }
    }

    /// <summary>
    /// Response from a batch API operation
    /// </summary>
    public class BatchApiResponse
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public List<ApiResponse> Responses { get; set; } = new List<ApiResponse>();
        public TimeSpan TotalProcessingTime { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool AllSuccessful => FailedRequests == 0;
        public double SuccessRate => TotalRequests == 0 ? 0 : (double)SuccessfulRequests / TotalRequests;
    }

    /// <summary>
    /// Represents a failed API request that needs retry
    /// </summary>
    public class FailedApiRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public MappedApiData? OriginalData { get; set; }
        public string EndpointName { get; set; } = string.Empty;
        public int AttemptCount { get; set; } = 0;
        public DateTime LastAttemptTime { get; set; } = DateTime.UtcNow;
        public string LastErrorMessage { get; set; } = string.Empty;
        public int LastStatusCode { get; set; }
        public RetryPolicy? RetryPolicy { get; set; }
        public DateTime NextRetryTime { get; set; }
        public bool ShouldRetry => AttemptCount < (RetryPolicy?.MaxAttempts ?? 3) && DateTime.UtcNow >= NextRetryTime;

        public FailedApiRequest() { }

        public FailedApiRequest(MappedApiData data, string endpointName)
        {
            MessageId = data.MessageId;
            OriginalData = data;
            EndpointName = endpointName;
            AttemptCount = 0;
            LastAttemptTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Configuration for retry behavior
    /// </summary>
    public class RetryPolicy
    {
        public int MaxAttempts { get; set; } = 3;
        public int BaseDelayMilliseconds { get; set; } = 1000;
        public bool UseExponentialBackoff { get; set; } = true;
        public double BackoffMultiplier { get; set; } = 2.0;
        public int MaxDelayMilliseconds { get; set; } = 30000;
        public List<int> RetryableStatusCodes { get; set; } = new List<int> { 429, 502, 503, 504 };
        public List<Type> RetryableExceptions { get; set; } = new List<Type>();

        public TimeSpan GetDelay(int attemptNumber)
        {
            if (!UseExponentialBackoff)
            {
                return TimeSpan.FromMilliseconds(BaseDelayMilliseconds);
            }

            var delay = BaseDelayMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber - 1);
            var delayMs = Math.Min(delay, MaxDelayMilliseconds);
            return TimeSpan.FromMilliseconds(delayMs);
        }

        public bool ShouldRetry(int statusCode, Exception? exception, int attemptNumber)
        {
            if (attemptNumber >= MaxAttempts)
                return false;

            if (RetryableStatusCodes.Contains(statusCode))
                return true;

            if (exception != null && RetryableExceptions.Any(type => type.IsAssignableFrom(exception.GetType())))
                return true;

            return false;
        }
    }

    /// <summary>
    /// Health check result for an API endpoint
    /// </summary>
    public class HealthCheckResult
    {
        public string EndpointName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        public static HealthCheckResult Healthy(string endpointName, TimeSpan responseTime, int statusCode = 200)
        {
            return new HealthCheckResult
            {
                EndpointName = endpointName,
                IsHealthy = true,
                ResponseTime = responseTime,
                StatusCode = statusCode,
                Message = "Endpoint is healthy"
            };
        }

        public static HealthCheckResult Unhealthy(string endpointName, string message, int statusCode = 0)
        {
            return new HealthCheckResult
            {
                EndpointName = endpointName,
                IsHealthy = false,
                StatusCode = statusCode,
                Message = message
            };
        }
    }

    /// <summary>
    /// Statistics for API endpoint performance
    /// </summary>
    public class ApiStatistics
    {
        public string EndpointName { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate => TotalRequests == 0 ? 0 : (double)SuccessfulRequests / TotalRequests;
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; } = TimeSpan.MaxValue;
        public TimeSpan MaxResponseTime { get; set; } = TimeSpan.Zero;
        public DateTime LastSuccessfulRequest { get; set; }
        public DateTime LastFailedRequest { get; set; }
        public DateTime FirstRequestTime { get; set; }
        public DateTime LastRequestTime { get; set; }
        public long TotalBytes { get; set; }
        public Dictionary<int, int> StatusCodeCounts { get; set; } = new Dictionary<int, int>();
        public List<HealthCheckResult> RecentHealthChecks { get; set; } = new List<HealthCheckResult>();

        public void RecordRequest(ApiResponse response)
        {
            TotalRequests++;
            LastRequestTime = DateTime.UtcNow;

            if (FirstRequestTime == default)
                FirstRequestTime = LastRequestTime;

            if (response.IsSuccess)
            {
                SuccessfulRequests++;
                LastSuccessfulRequest = LastRequestTime;
            }
            else
            {
                FailedRequests++;
                LastFailedRequest = LastRequestTime;
            }

            // Update response time statistics
            UpdateResponseTimeStats(response.ResponseTime);

            // Update status code counts
            if (StatusCodeCounts.ContainsKey(response.StatusCode))
                StatusCodeCounts[response.StatusCode]++;
            else
                StatusCodeCounts[response.StatusCode] = 1;

            // Estimate bytes transferred
            TotalBytes += response.ResponseContent?.Length ?? 0;
        }

        private void UpdateResponseTimeStats(TimeSpan responseTime)
        {
            if (responseTime < MinResponseTime)
                MinResponseTime = responseTime;

            if (responseTime > MaxResponseTime)
                MaxResponseTime = responseTime;

            // Calculate rolling average (simple approximation)
            var totalTime = AverageResponseTime.TotalMilliseconds * (TotalRequests - 1) + responseTime.TotalMilliseconds;
            AverageResponseTime = TimeSpan.FromMilliseconds(totalTime / TotalRequests);
        }

        public void AddHealthCheck(HealthCheckResult healthCheck)
        {
            RecentHealthChecks.Add(healthCheck);
            
            // Keep only the last 100 health checks
            if (RecentHealthChecks.Count > 100)
            {
                RecentHealthChecks.RemoveAt(0);
            }
        }
    }
}