using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SimpleSerialToApi.Services.Logging
{
    /// <summary>
    /// Extension methods for ILogger to provide structured logging for domain-specific operations
    /// </summary>
    public static class LoggerExtensions
    {
        // Serial Communication Logging
        public static void LogSerialCommunication(this ILogger logger, string port, string action, string? data = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Port"] = port,
                ["Action"] = action,
                ["Category"] = LogCategories.SerialCommunication
            }))
            {
                if (!string.IsNullOrEmpty(data))
                {
                    logger.LogInformation("Serial {Action} on {Port}: {Data}", action, port, data);
                }
                else
                {
                    logger.LogInformation("Serial {Action} on {Port}", action, port);
                }
            }
        }

        public static void LogSerialError(this ILogger logger, string port, string action, Exception exception, string? additionalContext = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Port"] = port,
                ["Action"] = action,
                ["Category"] = LogCategories.SerialCommunication
            }))
            {
                if (!string.IsNullOrEmpty(additionalContext))
                {
                    logger.LogError(exception, "Serial {Action} failed on {Port}. Context: {Context}", action, port, additionalContext);
                }
                else
                {
                    logger.LogError(exception, "Serial {Action} failed on {Port}", action, port);
                }
            }
        }

        // API Communication Logging
        public static void LogApiTransaction(this ILogger logger, string endpoint, string method, TimeSpan duration, bool success, int? statusCode = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Endpoint"] = endpoint,
                ["Method"] = method,
                ["Duration"] = duration.TotalMilliseconds,
                ["Category"] = LogCategories.ApiCommunication
            }))
            {
                if (statusCode.HasValue)
                {
                    logger.LogInformation("API {Method} {Endpoint} completed in {Duration}ms - Success: {Success}, StatusCode: {StatusCode}", 
                        method, endpoint, duration.TotalMilliseconds, success, statusCode.Value);
                }
                else
                {
                    logger.LogInformation("API {Method} {Endpoint} completed in {Duration}ms - Success: {Success}", 
                        method, endpoint, duration.TotalMilliseconds, success);
                }
            }
        }

        public static void LogApiError(this ILogger logger, string endpoint, string method, Exception exception, int? statusCode = null, string? responseContent = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Endpoint"] = endpoint,
                ["Method"] = method,
                ["Category"] = LogCategories.ApiCommunication
            }))
            {
                var message = statusCode.HasValue ? 
                    "API {Method} {Endpoint} failed with status {StatusCode}" :
                    "API {Method} {Endpoint} failed";

                if (!string.IsNullOrEmpty(responseContent))
                {
                    logger.LogError(exception, message + ". Response: {ResponseContent}", method, endpoint, statusCode, responseContent);
                }
                else if (statusCode.HasValue)
                {
                    logger.LogError(exception, message, method, endpoint, statusCode);
                }
                else
                {
                    logger.LogError(exception, message, method, endpoint);
                }
            }
        }

        // Queue Operations Logging
        public static void LogQueueOperation(this ILogger logger, string operation, int messageCount, string queueName)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["QueueName"] = queueName,
                ["MessageCount"] = messageCount,
                ["Category"] = LogCategories.DataProcessing
            }))
            {
                logger.LogDebug("Queue {Operation}: {MessageCount} messages in {QueueName}", 
                    operation, messageCount, queueName);
            }
        }

        public static void LogQueueError(this ILogger logger, string queueName, string operation, Exception exception, int? messageCount = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["QueueName"] = queueName,
                ["Operation"] = operation,
                ["Category"] = LogCategories.DataProcessing
            }))
            {
                if (messageCount.HasValue)
                {
                    logger.LogError(exception, "Queue {Operation} failed for {QueueName} with {MessageCount} messages", 
                        operation, queueName, messageCount.Value);
                }
                else
                {
                    logger.LogError(exception, "Queue {Operation} failed for {QueueName}", operation, queueName);
                }
            }
        }

        // Data Processing Logging
        public static void LogDataProcessing(this ILogger logger, string operation, string dataType, int? dataSize = null, TimeSpan? processingTime = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["DataType"] = dataType,
                ["Category"] = LogCategories.DataProcessing
            }))
            {
                var message = "Data {Operation} for {DataType}";
                var args = new List<object> { operation, dataType };

                if (dataSize.HasValue)
                {
                    message += " - Size: {DataSize} bytes";
                    args.Add(dataSize.Value);
                }

                if (processingTime.HasValue)
                {
                    message += " - Duration: {ProcessingTime}ms";
                    args.Add(processingTime.Value.TotalMilliseconds);
                }

                logger.LogInformation(message, args.ToArray());
            }
        }

        // Configuration Logging
        public static void LogConfigurationChange(this ILogger logger, string sectionName, string settingName, string? oldValue = null, string? newValue = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Section"] = sectionName,
                ["Setting"] = settingName,
                ["Category"] = LogCategories.Configuration
            }))
            {
                if (oldValue != null && newValue != null)
                {
                    logger.LogInformation("Configuration changed: {Section}.{Setting} from '{OldValue}' to '{NewValue}'", 
                        sectionName, settingName, oldValue, newValue);
                }
                else
                {
                    logger.LogInformation("Configuration loaded: {Section}.{Setting} = '{Value}'", 
                        sectionName, settingName, newValue ?? "null");
                }
            }
        }

        public static void LogConfigurationError(this ILogger logger, string sectionName, string settingName, Exception exception)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Section"] = sectionName,
                ["Setting"] = settingName,
                ["Category"] = LogCategories.Configuration
            }))
            {
                logger.LogError(exception, "Configuration error in {Section}.{Setting}", sectionName, settingName);
            }
        }

        // Performance Logging
        public static void LogPerformanceMetric(this ILogger logger, string metricName, double value, string? unit = null, string? context = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["MetricName"] = metricName,
                ["MetricValue"] = value,
                ["Category"] = LogCategories.Performance
            }))
            {
                var message = unit != null ? 
                    "Performance: {MetricName} = {Value} {Unit}" :
                    "Performance: {MetricName} = {Value}";

                var args = unit != null ? 
                    new object[] { metricName, value, unit } :
                    new object[] { metricName, value };

                if (!string.IsNullOrEmpty(context))
                {
                    message += " (Context: {Context})";
                    args = args.Concat(new[] { context }).ToArray();
                }

                logger.LogInformation(message, args);
            }
        }

        // Security Logging
        public static void LogSecurityEvent(this ILogger logger, string eventType, string description, string? userName = null, bool success = true)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["EventType"] = eventType,
                ["Success"] = success,
                ["Category"] = LogCategories.Security
            }))
            {
                var logLevel = success ? LogLevel.Information : LogLevel.Warning;
                
                if (!string.IsNullOrEmpty(userName))
                {
                    logger.Log(logLevel, "Security {EventType}: {Description} - User: {UserName}, Success: {Success}", 
                        eventType, description, userName, success);
                }
                else
                {
                    logger.Log(logLevel, "Security {EventType}: {Description} - Success: {Success}", 
                        eventType, description, success);
                }
            }
        }

        // User Interface Logging
        public static void LogUserAction(this ILogger logger, string action, string? component = null, string? details = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Action"] = action,
                ["Category"] = LogCategories.UserInterface
            }))
            {
                var message = "User {Action}";
                var args = new List<object> { action };

                if (!string.IsNullOrEmpty(component))
                {
                    message += " in {Component}";
                    args.Add(component);
                }

                if (!string.IsNullOrEmpty(details))
                {
                    message += " - {Details}";
                    args.Add(details);
                }

                logger.LogInformation(message, args.ToArray());
            }
        }

        // Application Lifecycle Logging
        public static void LogApplicationEvent(this ILogger logger, string eventType, string description, TimeSpan? duration = null)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["EventType"] = eventType,
                ["Category"] = "Application"
            }))
            {
                if (duration.HasValue)
                {
                    logger.LogInformation("Application {EventType}: {Description} - Duration: {Duration}ms", 
                        eventType, description, duration.Value.TotalMilliseconds);
                }
                else
                {
                    logger.LogInformation("Application {EventType}: {Description}", eventType, description);
                }
            }
        }
    }

    /// <summary>
    /// Predefined log categories for consistent logging
    /// </summary>
    public static class LogCategories
    {
        public const string SerialCommunication = "SerialComm";
        public const string ApiCommunication = "ApiComm";
        public const string DataProcessing = "DataProc";
        public const string Configuration = "Config";
        public const string UserInterface = "UI";
        public const string Performance = "Perf";
        public const string Security = "Security";
    }
}