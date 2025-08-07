using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services.Exceptions;
using SimpleSerialToApi.Services.Logging;
using SimpleSerialToApi.Interfaces;
using Polly;
using System.IO.Ports;

namespace SimpleSerialToApi.Services.Recovery
{
    /// <summary>
    /// Context information for recovery operations
    /// </summary>
    public class RecoveryContext
    {
        public string OperationName { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public int AttemptNumber { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public Exception? LastException { get; set; }
    }

    /// <summary>
    /// Result of a recovery operation
    /// </summary>
    public class RecoveryResult<T>
    {
        public bool Success { get; set; }
        public T? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public TimeSpan RecoveryTime { get; set; }
        public string? RecoveryStrategy { get; set; }
    }

    /// <summary>
    /// Interface for recovery strategies
    /// </summary>
    public interface IRecoveryStrategy<T>
    {
        /// <summary>
        /// Attempts to recover from the given exception
        /// </summary>
        Task<RecoveryResult<T>> AttemptRecoveryAsync(Exception exception, RecoveryContext context);

        /// <summary>
        /// Determines if this strategy can handle the given exception
        /// </summary>
        bool CanHandle(Exception exception);

        /// <summary>
        /// Maximum number of recovery attempts
        /// </summary>
        int MaxAttempts { get; }

        /// <summary>
        /// Name of the recovery strategy
        /// </summary>
        string StrategyName { get; }
    }

    /// <summary>
    /// Recovery strategy for serial connection issues
    /// </summary>
    public class SerialConnectionRecoveryStrategy : IRecoveryStrategy<bool>
    {
        private readonly ISerialCommunicationService _serialService;
        private readonly ILogger<SerialConnectionRecoveryStrategy> _logger;

        public int MaxAttempts => 5;
        public string StrategyName => "SerialConnectionRecovery";

        public SerialConnectionRecoveryStrategy(
            ISerialCommunicationService serialService,
            ILogger<SerialConnectionRecoveryStrategy> logger)
        {
            _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanHandle(Exception exception)
        {
            return exception is SerialCommunicationException ||
                   exception is InvalidOperationException invalidOpEx && invalidOpEx.Message.Contains("port") ||
                   exception is UnauthorizedAccessException ||
                   exception is IOException;
        }

        public async Task<RecoveryResult<bool>> AttemptRecoveryAsync(Exception exception, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Attempting serial connection recovery (attempt {AttemptNumber})", context.AttemptNumber);

            try
            {
                if (exception is SerialCommunicationException serialEx)
                {
                    return await HandleSerialException(serialEx, context);
                }

                if (exception is UnauthorizedAccessException)
                {
                    return await HandleUnauthorizedAccess(context);
                }

                if (exception is IOException ioEx)
                {
                    return await HandleIOException(ioEx, context);
                }

                // Generic recovery approach
                return await HandleGenericSerialError(exception, context);
            }
            catch (Exception recoveryException)
            {
                _logger.LogError(recoveryException, "Recovery attempt failed");
                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = $"Recovery failed: {recoveryException.Message}",
                    Exception = recoveryException,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
        }

        private async Task<RecoveryResult<bool>> HandleSerialException(SerialCommunicationException serialEx, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            switch (serialEx.ErrorType)
            {
                case SerialErrorType.PortNotFound:
                    // Try to find alternative ports
                    return await TryAlternativePorts(context);

                case SerialErrorType.PortAccessDenied:
                case SerialErrorType.PortAlreadyOpen:
                    // Wait and retry
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    return await TryReconnect(context);

                case SerialErrorType.CommunicationLost:
                case SerialErrorType.PortClosed:
                    // Attempt reconnection
                    return await TryReconnect(context);

                case SerialErrorType.ReadTimeout:
                case SerialErrorType.WriteTimeout:
                    // Adjust timeouts and retry
                    return await AdjustTimeoutsAndRetry(context);

                default:
                    return await HandleGenericSerialError(serialEx, context);
            }
        }

        private async Task<RecoveryResult<bool>> HandleUnauthorizedAccess(RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // Wait longer for port to become available
            await Task.Delay(TimeSpan.FromSeconds(5));
            
            try
            {
                var connected = await _serialService.ConnectAsync();
                return new RecoveryResult<bool>
                {
                    Success = connected,
                    Result = connected,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
            catch (Exception ex)
            {
                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = $"Still unauthorized after delay: {ex.Message}",
                    Exception = ex,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
        }

        private async Task<RecoveryResult<bool>> HandleIOException(IOException ioEx, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // IO exceptions often indicate hardware issues
            // Wait longer and try to re-establish connection
            await Task.Delay(TimeSpan.FromSeconds(3));
            
            return await TryReconnect(context, startTime);
        }

        private async Task<RecoveryResult<bool>> HandleGenericSerialError(Exception exception, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // Generic approach: wait and retry
            var delay = TimeSpan.FromSeconds(Math.Min(context.AttemptNumber * 2, 10));
            await Task.Delay(delay);
            
            return await TryReconnect(context, startTime);
        }

        private async Task<RecoveryResult<bool>> TryReconnect(RecoveryContext context, DateTime? startTime = null)
        {
            var actualStartTime = startTime ?? DateTime.UtcNow;
            
            try
            {
                // Disconnect first if connected
                if (_serialService.IsConnected)
                {
                    await _serialService.DisconnectAsync();
                    await Task.Delay(1000); // Give port time to close properly
                }

                var connected = await _serialService.ConnectAsync();
                
                _logger.LogSerialCommunication(
                    _serialService.ConnectionSettings.PortName, 
                    "RecoveryReconnect", 
                    connected ? "Success" : "Failed");

                return new RecoveryResult<bool>
                {
                    Success = connected,
                    Result = connected,
                    RecoveryTime = DateTime.UtcNow - actualStartTime,
                    RecoveryStrategy = StrategyName
                };
            }
            catch (Exception ex)
            {
                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = $"Reconnection failed: {ex.Message}",
                    Exception = ex,
                    RecoveryTime = DateTime.UtcNow - actualStartTime,
                    RecoveryStrategy = StrategyName
                };
            }
        }

        private async Task<RecoveryResult<bool>> TryAlternativePorts(RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var availablePorts = SerialPort.GetPortNames();
                var currentPort = _serialService.ConnectionSettings.PortName;
                
                _logger.LogInformation("Current port {CurrentPort} not available. Trying alternatives: {AvailablePorts}", 
                    currentPort, string.Join(", ", availablePorts));

                // Try other available ports
                foreach (var port in availablePorts)
                {
                    if (port.Equals(currentPort, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        // This would require updating the service to allow changing ports
                        // For now, we'll just log the attempt
                        _logger.LogInformation("Would try alternative port: {Port}", port);
                        
                        // In a real implementation, you might:
                        // 1. Update connection settings to use alternative port
                        // 2. Attempt connection
                        // 3. Return success if connected
                    }
                    catch (Exception portEx)
                    {
                        _logger.LogWarning(portEx, "Alternative port {Port} also failed", port);
                    }
                }

                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = "No alternative ports worked",
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
            catch (Exception ex)
            {
                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = $"Could not enumerate alternative ports: {ex.Message}",
                    Exception = ex,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
        }

        private async Task<RecoveryResult<bool>> AdjustTimeoutsAndRetry(RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // This would require the service to support dynamic timeout adjustment
                _logger.LogInformation("Would adjust timeouts and retry connection");
                
                // Simulate timeout adjustment and retry
                await Task.Delay(1000);
                
                var connected = await _serialService.ConnectAsync();
                
                return new RecoveryResult<bool>
                {
                    Success = connected,
                    Result = connected,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
            catch (Exception ex)
            {
                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = $"Timeout adjustment recovery failed: {ex.Message}",
                    Exception = ex,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
        }
    }

    /// <summary>
    /// Recovery strategy for API communication issues
    /// </summary>
    public class ApiConnectionRecoveryStrategy : IRecoveryStrategy<bool>
    {
        private readonly ILogger<ApiConnectionRecoveryStrategy> _logger;

        public int MaxAttempts => 3;
        public string StrategyName => "ApiConnectionRecovery";

        public ApiConnectionRecoveryStrategy(ILogger<ApiConnectionRecoveryStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanHandle(Exception exception)
        {
            return exception is ApiCommunicationException ||
                   exception is HttpRequestException ||
                   exception is TaskCanceledException ||
                   exception is TimeoutException;
        }

        public async Task<RecoveryResult<bool>> AttemptRecoveryAsync(Exception exception, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Attempting API connection recovery (attempt {AttemptNumber})", context.AttemptNumber);

            try
            {
                if (exception is ApiCommunicationException apiEx)
                {
                    return await HandleApiException(apiEx, context);
                }

                if (exception is HttpRequestException httpEx)
                {
                    return await HandleHttpException(httpEx, context);
                }

                if (exception is TaskCanceledException || exception is TimeoutException)
                {
                    return await HandleTimeoutException(exception, context);
                }

                return await HandleGenericApiError(exception, context);
            }
            catch (Exception recoveryException)
            {
                _logger.LogError(recoveryException, "API recovery attempt failed");
                return new RecoveryResult<bool>
                {
                    Success = false,
                    ErrorMessage = $"API recovery failed: {recoveryException.Message}",
                    Exception = recoveryException,
                    RecoveryTime = DateTime.UtcNow - startTime,
                    RecoveryStrategy = StrategyName
                };
            }
        }

        private async Task<RecoveryResult<bool>> HandleApiException(ApiCommunicationException apiEx, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // Base recovery strategy on status code
            var delay = apiEx.StatusCode switch
            {
                429 => TimeSpan.FromSeconds(60), // Rate limited - wait longer
                >= 500 => TimeSpan.FromSeconds(30), // Server error - moderate wait
                >= 400 => TimeSpan.FromSeconds(5), // Client error - short wait
                _ => TimeSpan.FromSeconds(10) // Unknown - default wait
            };

            _logger.LogApiError(apiEx.EndpointUrl, apiEx.HttpMethod, apiEx, apiEx.StatusCode, apiEx.ResponseContent);
            
            await Task.Delay(delay);
            
            return new RecoveryResult<bool>
            {
                Success = true, // Allow retry
                Result = true,
                RecoveryTime = DateTime.UtcNow - startTime,
                RecoveryStrategy = StrategyName
            };
        }

        private async Task<RecoveryResult<bool>> HandleHttpException(HttpRequestException httpEx, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // Network-related issues usually need a longer wait
            await Task.Delay(TimeSpan.FromSeconds(15));
            
            return new RecoveryResult<bool>
            {
                Success = true, // Allow retry
                Result = true,
                RecoveryTime = DateTime.UtcNow - startTime,
                RecoveryStrategy = StrategyName
            };
        }

        private async Task<RecoveryResult<bool>> HandleTimeoutException(Exception timeoutEx, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // Timeout issues - wait and allow retry with potentially longer timeout
            await Task.Delay(TimeSpan.FromSeconds(10));
            
            return new RecoveryResult<bool>
            {
                Success = true, // Allow retry
                Result = true,
                RecoveryTime = DateTime.UtcNow - startTime,
                RecoveryStrategy = StrategyName
            };
        }

        private async Task<RecoveryResult<bool>> HandleGenericApiError(Exception exception, RecoveryContext context)
        {
            var startTime = DateTime.UtcNow;
            
            // Generic delay based on attempt number
            var delay = TimeSpan.FromSeconds(Math.Min(context.AttemptNumber * 5, 30));
            await Task.Delay(delay);
            
            return new RecoveryResult<bool>
            {
                Success = true, // Allow retry
                Result = true,
                RecoveryTime = DateTime.UtcNow - startTime,
                RecoveryStrategy = StrategyName
            };
        }
    }

    /// <summary>
    /// Recovery manager that coordinates different recovery strategies
    /// </summary>
    public class RecoveryManager
    {
        private readonly ILogger<RecoveryManager> _logger;
        private readonly List<IRecoveryStrategy<bool>> _strategies;

        public RecoveryManager(
            ILogger<RecoveryManager> logger,
            IEnumerable<IRecoveryStrategy<bool>> strategies)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategies = strategies?.ToList() ?? new List<IRecoveryStrategy<bool>>();
        }

        /// <summary>
        /// Attempts recovery using appropriate strategy
        /// </summary>
        public async Task<RecoveryResult<T>> AttemptRecoveryAsync<T>(Exception exception, string operationName)
        {
            var context = new RecoveryContext
            {
                OperationName = operationName,
                AttemptNumber = 1,
                ElapsedTime = TimeSpan.Zero,
                LastException = exception
            };

            _logger.LogInformation("Starting recovery for operation {OperationName} due to {ExceptionType}", 
                operationName, exception.GetType().Name);

            foreach (var strategy in _strategies)
            {
                if (!strategy.CanHandle(exception))
                    continue;

                for (int attempt = 1; attempt <= strategy.MaxAttempts; attempt++)
                {
                    context.AttemptNumber = attempt;
                    var operationStart = DateTime.UtcNow;

                    try
                    {
                        var result = await strategy.AttemptRecoveryAsync(exception, context);
                        context.ElapsedTime += DateTime.UtcNow - operationStart;

                        if (result.Success)
                        {
                            _logger.LogInformation("Recovery successful using {StrategyName} after {AttemptCount} attempts", 
                                strategy.StrategyName, attempt);
                            
                            return new RecoveryResult<T>
                            {
                                Success = true,
                                Result = (T)(object)result.Result!,
                                RecoveryTime = context.ElapsedTime,
                                RecoveryStrategy = strategy.StrategyName
                            };
                        }

                        context.LastException = result.Exception ?? exception;
                        
                        if (attempt < strategy.MaxAttempts)
                        {
                            _logger.LogWarning("Recovery attempt {Attempt}/{MaxAttempts} failed: {ErrorMessage}", 
                                attempt, strategy.MaxAttempts, result.ErrorMessage);
                        }
                    }
                    catch (Exception strategyException)
                    {
                        _logger.LogError(strategyException, "Recovery strategy {StrategyName} attempt {Attempt} threw exception", 
                            strategy.StrategyName, attempt);
                        context.LastException = strategyException;
                    }
                }

                _logger.LogWarning("Recovery strategy {StrategyName} exhausted all {MaxAttempts} attempts", 
                    strategy.StrategyName, strategy.MaxAttempts);
            }

            _logger.LogError("All recovery strategies failed for operation {OperationName}", operationName);
            
            return new RecoveryResult<T>
            {
                Success = false,
                ErrorMessage = "All recovery strategies failed",
                Exception = context.LastException,
                RecoveryTime = context.ElapsedTime
            };
        }
    }
}