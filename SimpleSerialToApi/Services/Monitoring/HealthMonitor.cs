using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services.Logging;
using SimpleSerialToApi.Interfaces;
using System.Diagnostics;

namespace SimpleSerialToApi.Services.Monitoring
{
    /// <summary>
    /// Health status levels
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    /// <summary>
    /// Health status information
    /// </summary>
    public class HealthStatusInfo
    {
        public HealthStatus Status { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Component health status
    /// </summary>
    public class ComponentHealthStatus
    {
        public string ComponentName { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public string? Description { get; set; }
        public DateTime LastCheck { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// Health status changed event arguments
    /// </summary>
    public class HealthStatusChangedEventArgs : EventArgs
    {
        public string ComponentName { get; set; } = string.Empty;
        public HealthStatus OldStatus { get; set; }
        public HealthStatus NewStatus { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Interface for health checking components
    /// </summary>
    public interface IHealthChecker
    {
        /// <summary>
        /// Name of the component being monitored
        /// </summary>
        string ComponentName { get; }

        /// <summary>
        /// Checks the health of the component
        /// </summary>
        Task<HealthStatusInfo> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// How often this component should be checked (0 = manual only)
        /// </summary>
        TimeSpan CheckInterval { get; }
    }

    /// <summary>
    /// Interface for health monitoring
    /// </summary>
    public interface IHealthMonitor
    {
        /// <summary>
        /// Checks overall application health
        /// </summary>
        Task<HealthStatus> CheckApplicationHealthAsync();

        /// <summary>
        /// Checks health of a specific component
        /// </summary>
        Task<ComponentHealthStatus> CheckComponentHealthAsync(string componentName);

        /// <summary>
        /// Gets current status of all components
        /// </summary>
        Task<Dictionary<string, ComponentHealthStatus>> GetAllComponentStatusAsync();

        /// <summary>
        /// Event raised when health status changes
        /// </summary>
        event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        /// <summary>
        /// Starts continuous monitoring
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// Stops continuous monitoring
        /// </summary>
        Task StopMonitoringAsync();
    }

    /// <summary>
    /// Health checker for serial communication
    /// </summary>
    public class SerialHealthChecker : IHealthChecker
    {
        private readonly ISerialCommunicationService _serialService;
        private readonly ILogger<SerialHealthChecker> _logger;

        public string ComponentName => "SerialCommunication";
        public TimeSpan CheckInterval => TimeSpan.FromSeconds(30);

        public SerialHealthChecker(
            ISerialCommunicationService serialService,
            ILogger<SerialHealthChecker> logger)
        {
            _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthStatusInfo> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var healthInfo = new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Timestamp = DateTime.UtcNow
                };

                // Check connection status
                var isConnected = _serialService.IsConnected;
                healthInfo.Data["IsConnected"] = isConnected;
                healthInfo.Data["PortName"] = _serialService.ConnectionSettings.PortName;
                healthInfo.Data["BaudRate"] = _serialService.ConnectionSettings.BaudRate;

                if (!isConnected)
                {
                    // Try to determine why not connected
                    var availablePorts = System.IO.Ports.SerialPort.GetPortNames();
                    healthInfo.Data["AvailablePorts"] = availablePorts;
                    healthInfo.Data["ConfiguredPortExists"] = availablePorts.Contains(_serialService.ConnectionSettings.PortName);
                    
                    healthInfo.Status = HealthStatus.Unhealthy;
                    healthInfo.Description = $"Serial port {_serialService.ConnectionSettings.PortName} is not connected";
                    
                    // Check if configured port exists
                    if (!availablePorts.Contains(_serialService.ConnectionSettings.PortName))
                    {
                        healthInfo.Description += " and port does not exist";
                    }
                }
                else
                {
                    // Connected - check communication quality
                    healthInfo.Status = HealthStatus.Healthy;
                    healthInfo.Description = $"Serial port {_serialService.ConnectionSettings.PortName} is connected and operational";
                }

                healthInfo.ResponseTime = stopwatch.Elapsed;
                return healthInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking serial communication health");
                
                return new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Status = HealthStatus.Unknown,
                    Description = $"Failed to check serial health: {ex.Message}",
                    Exception = ex,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
    }

    /// <summary>
    /// Health checker for API communication
    /// </summary>
    public class ApiHealthChecker : IHealthChecker
    {
        private readonly ILogger<ApiHealthChecker> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _healthCheckUrl;

        public string ComponentName => "ApiCommunication";
        public TimeSpan CheckInterval => TimeSpan.FromMinutes(1);

        public ApiHealthChecker(
            ILogger<ApiHealthChecker> logger,
            HttpClient httpClient,
            string healthCheckUrl = "https://httpbin.org/status/200") // Default test endpoint
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _healthCheckUrl = healthCheckUrl ?? throw new ArgumentNullException(nameof(healthCheckUrl));
        }

        public async Task<HealthStatusInfo> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var healthInfo = new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Timestamp = DateTime.UtcNow
                };

                // Perform simple HTTP health check
                using var response = await _httpClient.GetAsync(_healthCheckUrl, cancellationToken);
                
                healthInfo.Data["StatusCode"] = (int)response.StatusCode;
                healthInfo.Data["ReasonPhrase"] = response.ReasonPhrase ?? string.Empty;
                healthInfo.Data["HealthCheckUrl"] = _healthCheckUrl;
                healthInfo.ResponseTime = stopwatch.Elapsed;

                if (response.IsSuccessStatusCode)
                {
                    healthInfo.Status = HealthStatus.Healthy;
                    healthInfo.Description = $"API communication is healthy (Response: {response.StatusCode})";
                }
                else if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    healthInfo.Status = HealthStatus.Degraded;
                    healthInfo.Description = $"API communication has client errors (Response: {response.StatusCode})";
                }
                else
                {
                    healthInfo.Status = HealthStatus.Unhealthy;
                    healthInfo.Description = $"API communication is unhealthy (Response: {response.StatusCode})";
                }

                return healthInfo;
            }
            catch (TaskCanceledException)
            {
                return new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Status = HealthStatus.Unhealthy,
                    Description = "API health check timed out",
                    ResponseTime = stopwatch.Elapsed
                };
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error during API health check");
                
                return new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Status = HealthStatus.Unhealthy,
                    Description = $"API communication error: {httpEx.Message}",
                    Exception = httpEx,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API communication health");
                
                return new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Status = HealthStatus.Unknown,
                    Description = $"Failed to check API health: {ex.Message}",
                    Exception = ex,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
    }

    /// <summary>
    /// Health checker for message queue system
    /// </summary>
    public class QueueHealthChecker : IHealthChecker
    {
        private readonly IQueueManager _queueManager;
        private readonly ILogger<QueueHealthChecker> _logger;

        public string ComponentName => "MessageQueue";
        public TimeSpan CheckInterval => TimeSpan.FromSeconds(15);

        public QueueHealthChecker(
            IQueueManager queueManager,
            ILogger<QueueHealthChecker> logger)
        {
            _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthStatusInfo> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var healthInfo = new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Timestamp = DateTime.UtcNow
                };

                // Check queue statistics
                var queueCount = _queueManager.GetQueueCount();
                var totalMessages = 0;
                var highestUtilization = 0.0;
                var queueDetails = new Dictionary<string, object>();

                foreach (var queueName in _queueManager.GetQueueNames())
                {
                    try
                    {
                        var count = _queueManager.GetMessageCount(queueName);
                        var capacity = _queueManager.GetQueueCapacity(queueName);
                        var utilization = capacity > 0 ? (double)count / capacity : 0.0;
                        
                        totalMessages += count;
                        highestUtilization = Math.Max(highestUtilization, utilization);
                        
                        queueDetails[queueName] = new
                        {
                            MessageCount = count,
                            Capacity = capacity,
                            Utilization = utilization
                        };
                    }
                    catch (Exception queueEx)
                    {
                        _logger.LogWarning(queueEx, "Error checking queue {QueueName}", queueName);
                        queueDetails[queueName] = new { Error = queueEx.Message };
                    }
                }

                healthInfo.Data["QueueCount"] = queueCount;
                healthInfo.Data["TotalMessages"] = totalMessages;
                healthInfo.Data["HighestUtilization"] = highestUtilization;
                healthInfo.Data["QueueDetails"] = queueDetails;
                healthInfo.ResponseTime = stopwatch.Elapsed;

                // Determine health status based on utilization
                if (highestUtilization > 0.9)
                {
                    healthInfo.Status = HealthStatus.Unhealthy;
                    healthInfo.Description = $"Message queues are critically full (Utilization: {highestUtilization:P})";
                }
                else if (highestUtilization > 0.7)
                {
                    healthInfo.Status = HealthStatus.Degraded;
                    healthInfo.Description = $"Message queues are getting full (Utilization: {highestUtilization:P})";
                }
                else
                {
                    healthInfo.Status = HealthStatus.Healthy;
                    healthInfo.Description = $"Message queues are healthy ({queueCount} queues, {totalMessages} total messages)";
                }

                return healthInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking queue health");
                
                return new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Status = HealthStatus.Unknown,
                    Description = $"Failed to check queue health: {ex.Message}",
                    Exception = ex,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
    }

    /// <summary>
    /// System resource health checker
    /// </summary>
    public class SystemResourceHealthChecker : IHealthChecker
    {
        private readonly ILogger<SystemResourceHealthChecker> _logger;

        public string ComponentName => "SystemResources";
        public TimeSpan CheckInterval => TimeSpan.FromMinutes(2);

        public SystemResourceHealthChecker(ILogger<SystemResourceHealthChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthStatusInfo> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var healthInfo = new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Timestamp = DateTime.UtcNow
                };

                var process = Process.GetCurrentProcess();
                
                // Memory usage
                var workingSetMB = process.WorkingSet64 / (1024 * 1024);
                var privateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024);
                
                // CPU usage (approximate)
                var totalProcessorTime = process.TotalProcessorTime;
                await Task.Delay(100, cancellationToken); // Small delay to measure CPU
                process.Refresh();
                var cpuUsage = (process.TotalProcessorTime - totalProcessorTime).TotalMilliseconds / 100.0;

                healthInfo.Data["WorkingSetMB"] = workingSetMB;
                healthInfo.Data["PrivateMemoryMB"] = privateMemoryMB;
                healthInfo.Data["CpuUsagePercent"] = Math.Round(cpuUsage, 2);
                healthInfo.Data["ThreadCount"] = process.Threads.Count;
                healthInfo.Data["HandleCount"] = process.HandleCount;
                healthInfo.ResponseTime = stopwatch.Elapsed;

                // Determine health based on resource usage
                if (workingSetMB > 500 || cpuUsage > 80)
                {
                    healthInfo.Status = HealthStatus.Unhealthy;
                    healthInfo.Description = $"High resource usage (Memory: {workingSetMB}MB, CPU: {cpuUsage:F1}%)";
                }
                else if (workingSetMB > 250 || cpuUsage > 50)
                {
                    healthInfo.Status = HealthStatus.Degraded;
                    healthInfo.Description = $"Moderate resource usage (Memory: {workingSetMB}MB, CPU: {cpuUsage:F1}%)";
                }
                else
                {
                    healthInfo.Status = HealthStatus.Healthy;
                    healthInfo.Description = $"System resources are healthy (Memory: {workingSetMB}MB, CPU: {cpuUsage:F1}%)";
                }

                return healthInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system resource health");
                
                return new HealthStatusInfo
                {
                    ComponentName = ComponentName,
                    Status = HealthStatus.Unknown,
                    Description = $"Failed to check system resources: {ex.Message}",
                    Exception = ex,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
    }

    /// <summary>
    /// Main health monitor implementation
    /// </summary>
    public class ApplicationHealthMonitor : IHealthMonitor
    {
        private readonly IEnumerable<IHealthChecker> _healthCheckers;
        private readonly ILogger<ApplicationHealthMonitor> _logger;
        private readonly Dictionary<string, ComponentHealthStatus> _componentStatuses;
        private readonly Dictionary<string, Timer> _componentTimers;
        private readonly object _statusLock = new object();
        private bool _isMonitoring = false;

        public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

        public ApplicationHealthMonitor(
            IEnumerable<IHealthChecker> healthCheckers,
            ILogger<ApplicationHealthMonitor> logger)
        {
            _healthCheckers = healthCheckers ?? throw new ArgumentNullException(nameof(healthCheckers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _componentStatuses = new Dictionary<string, ComponentHealthStatus>();
            _componentTimers = new Dictionary<string, Timer>();
        }

        public async Task<HealthStatus> CheckApplicationHealthAsync()
        {
            try
            {
                var componentStatuses = await GetAllComponentStatusAsync();
                
                if (!componentStatuses.Any())
                    return HealthStatus.Unknown;

                var unhealthyCount = componentStatuses.Count(kvp => kvp.Value.Status == HealthStatus.Unhealthy);
                var degradedCount = componentStatuses.Count(kvp => kvp.Value.Status == HealthStatus.Degraded);
                var healthyCount = componentStatuses.Count(kvp => kvp.Value.Status == HealthStatus.Healthy);

                // Overall health logic
                if (unhealthyCount > 0)
                {
                    return unhealthyCount > componentStatuses.Count / 2 ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                if (degradedCount > 0)
                {
                    return degradedCount > componentStatuses.Count / 2 ? HealthStatus.Degraded : HealthStatus.Healthy;
                }

                return healthyCount > 0 ? HealthStatus.Healthy : HealthStatus.Unknown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overall application health");
                return HealthStatus.Unknown;
            }
        }

        public async Task<ComponentHealthStatus> CheckComponentHealthAsync(string componentName)
        {
            var checker = _healthCheckers.FirstOrDefault(c => c.ComponentName.Equals(componentName, StringComparison.OrdinalIgnoreCase));
            if (checker == null)
            {
                return new ComponentHealthStatus
                {
                    ComponentName = componentName,
                    Status = HealthStatus.Unknown,
                    Description = "Component not found",
                    LastCheck = DateTime.UtcNow
                };
            }

            try
            {
                var healthInfo = await checker.CheckHealthAsync();
                var componentStatus = new ComponentHealthStatus
                {
                    ComponentName = checker.ComponentName,
                    Status = healthInfo.Status,
                    Description = healthInfo.Description,
                    LastCheck = healthInfo.Timestamp,
                    Metrics = healthInfo.Data
                };

                // Update stored status and notify if changed
                UpdateComponentStatus(componentStatus);

                return componentStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for component {ComponentName}", componentName);
                
                var errorStatus = new ComponentHealthStatus
                {
                    ComponentName = checker.ComponentName,
                    Status = HealthStatus.Unknown,
                    Description = $"Health check failed: {ex.Message}",
                    LastCheck = DateTime.UtcNow,
                    Issues = new List<string> { ex.Message }
                };

                UpdateComponentStatus(errorStatus);
                return errorStatus;
            }
        }

        public async Task<Dictionary<string, ComponentHealthStatus>> GetAllComponentStatusAsync()
        {
            var results = new Dictionary<string, ComponentHealthStatus>();
            var tasks = _healthCheckers.Select(async checker =>
            {
                var status = await CheckComponentHealthAsync(checker.ComponentName);
                return new KeyValuePair<string, ComponentHealthStatus>(checker.ComponentName, status);
            });

            var completedTasks = await Task.WhenAll(tasks);
            foreach (var kvp in completedTasks)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
                return;

            _logger.LogApplicationEvent("StartMonitoring", "Health monitoring started");
            _isMonitoring = true;

            foreach (var checker in _healthCheckers)
            {
                if (checker.CheckInterval > TimeSpan.Zero)
                {
                    var timer = new Timer(async _ => await CheckComponentHealthAsync(checker.ComponentName),
                        null, TimeSpan.Zero, checker.CheckInterval);
                    
                    _componentTimers[checker.ComponentName] = timer;
                }
            }
        }

        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
                return;

            _logger.LogApplicationEvent("StopMonitoring", "Health monitoring stopped");
            _isMonitoring = false;

            foreach (var timer in _componentTimers.Values)
            {
                timer?.Dispose();
            }
            _componentTimers.Clear();
        }

        private void UpdateComponentStatus(ComponentHealthStatus newStatus)
        {
            lock (_statusLock)
            {
                var hasExisting = _componentStatuses.TryGetValue(newStatus.ComponentName, out var existing);
                var statusChanged = !hasExisting || existing.Status != newStatus.Status;

                _componentStatuses[newStatus.ComponentName] = newStatus;

                if (statusChanged)
                {
                    var args = new HealthStatusChangedEventArgs
                    {
                        ComponentName = newStatus.ComponentName,
                        OldStatus = existing?.Status ?? HealthStatus.Unknown,
                        NewStatus = newStatus.Status,
                        Description = newStatus.Description
                    };

                    _logger.LogInformation("Health status changed for {ComponentName}: {OldStatus} -> {NewStatus}", 
                        args.ComponentName, args.OldStatus, args.NewStatus);

                    HealthStatusChanged?.Invoke(this, args);
                }
            }
        }
    }
}