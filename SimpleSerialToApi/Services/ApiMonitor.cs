using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// API monitoring service that tracks statistics and health for endpoints
    /// </summary>
    public class ApiMonitor : IApiMonitor
    {
        private readonly ILogger<ApiMonitor> _logger;
        private readonly ConcurrentDictionary<string, ApiStatistics> _statistics;
        private readonly object _lock = new object();

        public ApiMonitor(ILogger<ApiMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statistics = new ConcurrentDictionary<string, ApiStatistics>();
        }

        public void RecordApiCall(string endpointName, object request, ApiResponse response)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            if (response == null)
                throw new ArgumentNullException(nameof(response));

            var stats = _statistics.GetOrAdd(endpointName, name => new ApiStatistics { EndpointName = name });

            lock (_lock)
            {
                stats.RecordRequest(response);
                _logger.LogTrace("Recorded API call for endpoint {EndpointName}: {StatusCode} in {ResponseTime}ms", 
                    endpointName, response.StatusCode, response.ResponseTime.TotalMilliseconds);
            }
        }

        public ApiStatistics GetStatistics(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            return _statistics.GetOrAdd(endpointName, name => new ApiStatistics { EndpointName = name });
        }

        public Dictionary<string, ApiStatistics> GetAllStatistics()
        {
            var result = new Dictionary<string, ApiStatistics>();
            foreach (var kvp in _statistics)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        public void ResetStatistics(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            if (_statistics.TryRemove(endpointName, out var oldStats))
            {
                var newStats = new ApiStatistics { EndpointName = endpointName };
                _statistics.TryAdd(endpointName, newStats);
                
                _logger.LogInformation("Reset statistics for endpoint {EndpointName}", endpointName);
            }
        }

        public void ResetAllStatistics()
        {
            var endpointNames = new List<string>(_statistics.Keys);
            _statistics.Clear();

            foreach (var endpointName in endpointNames)
            {
                _statistics.TryAdd(endpointName, new ApiStatistics { EndpointName = endpointName });
            }

            _logger.LogInformation("Reset statistics for all endpoints ({Count} endpoints)", endpointNames.Count);
        }

        public bool IsEndpointHealthy(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            var stats = GetStatistics(endpointName);

            // Consider endpoint healthy if:
            // 1. Success rate is above 80%
            // 2. Average response time is under 10 seconds
            // 3. We've had recent successful requests (within last 10 minutes)
            var isHealthy = stats.SuccessRate >= 0.8 &&
                           stats.AverageResponseTime.TotalSeconds <= 10 &&
                           (stats.LastSuccessfulRequest == default || 
                            DateTime.UtcNow - stats.LastSuccessfulRequest <= TimeSpan.FromMinutes(10));

            // Check recent health checks if available
            if (stats.RecentHealthChecks.Count > 0)
            {
                var recentHealthCheck = stats.RecentHealthChecks
                    .Where(hc => DateTime.UtcNow - hc.CheckTime <= TimeSpan.FromMinutes(5))
                    .OrderByDescending(hc => hc.CheckTime)
                    .FirstOrDefault();

                if (recentHealthCheck != null)
                {
                    isHealthy = isHealthy && recentHealthCheck.IsHealthy;
                }
            }

            _logger.LogTrace("Health check for endpoint {EndpointName}: {IsHealthy} (SuccessRate: {SuccessRate:P2}, AvgResponseTime: {AvgResponseTime}ms)", 
                endpointName, isHealthy, stats.SuccessRate, stats.AverageResponseTime.TotalMilliseconds);

            return isHealthy;
        }

        /// <summary>
        /// Get health summary for all monitored endpoints
        /// </summary>
        /// <returns>Dictionary of endpoint health status</returns>
        public Dictionary<string, bool> GetHealthSummary()
        {
            var healthSummary = new Dictionary<string, bool>();
            
            foreach (var kvp in _statistics)
            {
                healthSummary[kvp.Key] = IsEndpointHealthy(kvp.Key);
            }

            return healthSummary;
        }

        /// <summary>
        /// Get performance summary for all monitored endpoints
        /// </summary>
        /// <returns>Performance metrics summary</returns>
        public Dictionary<string, object> GetPerformanceSummary()
        {
            var summary = new Dictionary<string, object>();

            var allStats = GetAllStatistics();
            if (allStats.Count == 0)
            {
                return summary;
            }

            var totalRequests = allStats.Values.Sum(s => s.TotalRequests);
            var totalSuccessful = allStats.Values.Sum(s => s.SuccessfulRequests);
            var totalFailed = allStats.Values.Sum(s => s.FailedRequests);
            var overallSuccessRate = totalRequests == 0 ? 0 : (double)totalSuccessful / totalRequests;

            var avgResponseTimes = allStats.Values
                .Where(s => s.AverageResponseTime > TimeSpan.Zero)
                .Select(s => s.AverageResponseTime.TotalMilliseconds)
                .ToList();

            var overallAvgResponseTime = avgResponseTimes.Count > 0 ? avgResponseTimes.Average() : 0;

            summary["TotalRequests"] = totalRequests;
            summary["TotalSuccessful"] = totalSuccessful;
            summary["TotalFailed"] = totalFailed;
            summary["OverallSuccessRate"] = overallSuccessRate;
            summary["AverageResponseTimeMs"] = overallAvgResponseTime;
            summary["MonitoredEndpoints"] = allStats.Count;
            summary["HealthyEndpoints"] = GetHealthSummary().Values.Count(h => h);

            return summary;
        }

        /// <summary>
        /// Get recent activity for an endpoint (last 24 hours)
        /// </summary>
        /// <param name="endpointName">Name of the endpoint</param>
        /// <param name="hours">Number of hours to look back (default: 24)</param>
        /// <returns>Recent activity metrics</returns>
        public Dictionary<string, object> GetRecentActivity(string endpointName, int hours = 24)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("Endpoint name cannot be null or empty", nameof(endpointName));

            var stats = GetStatistics(endpointName);
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            var recentActivity = new Dictionary<string, object>
            {
                ["EndpointName"] = endpointName,
                ["PeriodHours"] = hours,
                ["CutoffTime"] = cutoffTime
            };

            // Calculate activity within the specified time window
            // Note: This is a simplified implementation. In a production system,
            // you'd want to track timestamped request data for more accurate recent activity metrics.
            
            var hasRecentActivity = (stats.LastRequestTime != default && stats.LastRequestTime >= cutoffTime) ||
                                   (stats.LastSuccessfulRequest != default && stats.LastSuccessfulRequest >= cutoffTime) ||
                                   (stats.LastFailedRequest != default && stats.LastFailedRequest >= cutoffTime);

            recentActivity["HasRecentActivity"] = hasRecentActivity;
            recentActivity["LastRequestTime"] = stats.LastRequestTime;
            recentActivity["LastSuccessfulRequest"] = stats.LastSuccessfulRequest;
            recentActivity["LastFailedRequest"] = stats.LastFailedRequest;

            // Recent health checks within the time window
            var recentHealthChecks = stats.RecentHealthChecks
                .Where(hc => hc.CheckTime >= cutoffTime)
                .OrderByDescending(hc => hc.CheckTime)
                .ToList();

            recentActivity["RecentHealthChecks"] = recentHealthChecks.Count;
            recentActivity["LastHealthCheck"] = recentHealthChecks.FirstOrDefault()?.CheckTime ?? default(DateTime?);
            recentActivity["HealthyInPeriod"] = recentHealthChecks.All(hc => hc.IsHealthy);

            return recentActivity;
        }
    }
}