using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services.Monitoring;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SimpleSerialToApi.Services.Diagnostics
{
    /// <summary>
    /// Comprehensive diagnostic report for troubleshooting
    /// </summary>
    public class DiagnosticReport
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ApplicationVersion { get; set; } = string.Empty;
        public SystemInformation SystemInfo { get; set; } = new();
        public ApplicationConfiguration ConfigurationSnapshot { get; set; } = new();
        public List<LogEntry> RecentLogs { get; set; } = new();
        public ExceptionDetails? ExceptionDetails { get; set; }
        public PerformanceCounters PerformanceCounters { get; set; } = new();
        public List<ComponentHealthStatus> HealthStatuses { get; set; } = new();
        public NetworkDiagnostics NetworkInfo { get; set; } = new();
        public string ReportId { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== DIAGNOSTIC REPORT ===");
            sb.AppendLine($"Report ID: {ReportId}");
            sb.AppendLine($"Generated: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Application: {ApplicationVersion}");
            sb.AppendLine();

            if (ExceptionDetails != null)
            {
                sb.AppendLine("=== EXCEPTION DETAILS ===");
                sb.AppendLine($"Type: {ExceptionDetails.ExceptionType}");
                sb.AppendLine($"Message: {ExceptionDetails.Message}");
                sb.AppendLine($"Stack Trace:");
                sb.AppendLine(ExceptionDetails.StackTrace);
                sb.AppendLine();
            }

            sb.AppendLine("=== SYSTEM INFORMATION ===");
            sb.AppendLine($"Machine: {SystemInfo.MachineName}");
            sb.AppendLine($"OS: {SystemInfo.OSDescription}");
            sb.AppendLine($"Runtime: {SystemInfo.RuntimeVersion}");
            sb.AppendLine($"Architecture: {SystemInfo.ProcessorArchitecture}");
            sb.AppendLine($"Memory: {SystemInfo.TotalPhysicalMemoryMB} MB total, {SystemInfo.AvailablePhysicalMemoryMB} MB available");
            sb.AppendLine();

            sb.AppendLine("=== PERFORMANCE COUNTERS ===");
            sb.AppendLine($"CPU Usage: {PerformanceCounters.CpuUsagePercent:F1}%");
            sb.AppendLine($"Working Set: {PerformanceCounters.WorkingSetMB} MB");
            sb.AppendLine($"Private Memory: {PerformanceCounters.PrivateMemoryMB} MB");
            sb.AppendLine($"Threads: {PerformanceCounters.ThreadCount}");
            sb.AppendLine($"Handles: {PerformanceCounters.HandleCount}");
            sb.AppendLine();

            if (HealthStatuses.Any())
            {
                sb.AppendLine("=== COMPONENT HEALTH ===");
                foreach (var health in HealthStatuses)
                {
                    sb.AppendLine($"{health.ComponentName}: {health.Status} - {health.Description}");
                }
                sb.AppendLine();
            }

            if (RecentLogs.Any())
            {
                sb.AppendLine("=== RECENT LOGS (Last 10) ===");
                foreach (var log in RecentLogs.TakeLast(10))
                {
                    sb.AppendLine($"[{log.Timestamp:HH:mm:ss.fff}] {log.Level} - {log.Message}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public class SystemInformation
    {
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public string OSDescription { get; set; } = RuntimeInformation.OSDescription;
        public string RuntimeVersion { get; set; } = RuntimeInformation.FrameworkDescription;
        public string ProcessorArchitecture { get; set; } = RuntimeInformation.ProcessArchitecture.ToString();
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
        public long TotalPhysicalMemoryMB { get; set; }
        public long AvailablePhysicalMemoryMB { get; set; }
        public DateTime SystemStartTime { get; set; }
        public TimeSpan SystemUptime { get; set; }
    }

    public class ApplicationConfiguration
    {
        public Dictionary<string, string> AppSettings { get; set; } = new();
        public Dictionary<string, object> ConnectionSettings { get; set; } = new();
        public List<string> LoadedAssemblies { get; set; } = new();
        public string CurrentDirectory { get; set; } = Environment.CurrentDirectory;
        public string ApplicationPath { get; set; } = string.Empty;
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Exception { get; set; }
    }

    public class ExceptionDetails
    {
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public List<InnerExceptionInfo> InnerExceptions { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class InnerExceptionInfo
    {
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PerformanceCounters
    {
        public double CpuUsagePercent { get; set; }
        public long WorkingSetMB { get; set; }
        public long PrivateMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long TotalProcessorTimeMS { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    public class NetworkDiagnostics
    {
        public bool IsNetworkAvailable { get; set; }
        public List<string> NetworkInterfaces { get; set; } = new();
        public Dictionary<string, bool> EndpointConnectivity { get; set; } = new();
        public string? PublicIPAddress { get; set; }
    }

    /// <summary>
    /// Service for generating comprehensive diagnostic reports
    /// </summary>
    public class DiagnosticReportGenerator
    {
        private readonly ILogger<DiagnosticReportGenerator> _logger;
        private readonly IHealthMonitor? _healthMonitor;
        private readonly List<LogEntry> _recentLogs;
        private readonly object _logLock = new object();

        public DiagnosticReportGenerator(
            ILogger<DiagnosticReportGenerator> logger,
            IHealthMonitor? healthMonitor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthMonitor = healthMonitor;
            _recentLogs = new List<LogEntry>();
        }

        /// <summary>
        /// Adds a log entry to the recent logs collection
        /// </summary>
        public void AddLogEntry(LogEntry logEntry)
        {
            lock (_logLock)
            {
                _recentLogs.Add(logEntry);
                
                // Keep only last 1000 entries
                if (_recentLogs.Count > 1000)
                {
                    _recentLogs.RemoveRange(0, _recentLogs.Count - 1000);
                }
            }
        }

        /// <summary>
        /// Generates a comprehensive diagnostic report
        /// </summary>
        public async Task<DiagnosticReport> GenerateReportAsync(Exception? exception = null)
        {
            try
            {
                var report = new DiagnosticReport
                {
                    ApplicationVersion = GetApplicationVersion(),
                    SystemInfo = await GetSystemInformationAsync(),
                    ConfigurationSnapshot = await GetConfigurationSnapshotAsync(),
                    RecentLogs = await GetRecentLogsAsync(TimeSpan.FromMinutes(10)),
                    ExceptionDetails = exception != null ? GetExceptionDetails(exception) : null,
                    PerformanceCounters = GetPerformanceCounters(),
                    NetworkInfo = await GetNetworkDiagnosticsAsync()
                };

                // Get health statuses if health monitor is available
                if (_healthMonitor != null)
                {
                    try
                    {
                        var healthStatuses = await _healthMonitor.GetAllComponentStatusAsync();
                        report.HealthStatuses = healthStatuses.Values.ToList();
                    }
                    catch (Exception healthEx)
                    {
                        _logger.LogWarning(healthEx, "Could not retrieve health statuses for diagnostic report");
                    }
                }

                _logger.LogInformation("Generated diagnostic report {ReportId} with {ComponentCount} health components and {LogCount} log entries",
                    report.ReportId, report.HealthStatuses.Count, report.RecentLogs.Count);

                return report;
            }
            catch (Exception reportEx)
            {
                _logger.LogError(reportEx, "Error generating diagnostic report");
                
                // Return minimal report with error info
                return new DiagnosticReport
                {
                    ApplicationVersion = GetApplicationVersion(),
                    ExceptionDetails = GetExceptionDetails(reportEx),
                    RecentLogs = new List<LogEntry>
                    {
                        new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "ERROR",
                            Message = $"Failed to generate diagnostic report: {reportEx.Message}",
                            Exception = reportEx.ToString()
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Exports the diagnostic report to a file
        /// </summary>
        public async Task<string> ExportReportAsync(DiagnosticReport report, string? filePath = null)
        {
            try
            {
                filePath ??= Path.Combine(Path.GetTempPath(), $"diagnostic_report_{report.ReportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");

                await File.WriteAllTextAsync(filePath, report.ToString());
                
                _logger.LogInformation("Diagnostic report exported to {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting diagnostic report to {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Exports the diagnostic report as JSON
        /// </summary>
        public async Task<string> ExportReportAsJsonAsync(DiagnosticReport report, string? filePath = null)
        {
            try
            {
                filePath ??= Path.Combine(Path.GetTempPath(), $"diagnostic_report_{report.ReportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(report, options);
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("Diagnostic report exported as JSON to {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting diagnostic report as JSON to {FilePath}", filePath);
                throw;
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var productName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "SimpleSerialToApi";
                
                return $"{productName} v{version}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<SystemInformation> GetSystemInformationAsync()
        {
            try
            {
                var systemInfo = new SystemInformation();

                // Get memory information (Windows specific)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using var pc = new PerformanceCounter("Memory", "Available MBytes");
                        systemInfo.AvailablePhysicalMemoryMB = (long)pc.NextValue();
                        
                        // Estimate total memory (this is approximate)
                        systemInfo.TotalPhysicalMemoryMB = systemInfo.AvailablePhysicalMemoryMB + (Environment.WorkingSet / (1024 * 1024));
                    }
                    catch
                    {
                        // Fallback to working set
                        systemInfo.TotalPhysicalMemoryMB = Environment.WorkingSet / (1024 * 1024);
                        systemInfo.AvailablePhysicalMemoryMB = systemInfo.TotalPhysicalMemoryMB / 2; // Rough estimate
                    }
                }

                systemInfo.SystemStartTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);
                systemInfo.SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

                return systemInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error gathering system information");
                return new SystemInformation();
            }
        }

        private async Task<ApplicationConfiguration> GetConfigurationSnapshotAsync()
        {
            try
            {
                var config = new ApplicationConfiguration();

                // Get app settings from configuration (this would need to be injected in real implementation)
                // For now, we'll get environment variables as an example
                foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
                {
                    var key = envVar.Key?.ToString();
                    var value = envVar.Value?.ToString();
                    
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        // Only include non-sensitive environment variables
                        if (!key.ToUpperInvariant().Contains("PASSWORD") && 
                            !key.ToUpperInvariant().Contains("TOKEN") &&
                            !key.ToUpperInvariant().Contains("SECRET"))
                        {
                            config.AppSettings[key] = value;
                        }
                    }
                }

                // Get loaded assemblies
                config.LoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => $"{a.GetName().Name} v{a.GetName().Version}")
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToList();

                var entryAssembly = Assembly.GetEntryAssembly();
                config.ApplicationPath = entryAssembly?.Location ?? "Unknown";

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error gathering configuration snapshot");
                return new ApplicationConfiguration();
            }
        }

        private async Task<List<LogEntry>> GetRecentLogsAsync(TimeSpan timeWindow)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - timeWindow;
                
                lock (_logLock)
                {
                    return _recentLogs
                        .Where(log => log.Timestamp >= cutoffTime)
                        .OrderBy(log => log.Timestamp)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving recent logs");
                return new List<LogEntry>();
            }
        }

        private ExceptionDetails GetExceptionDetails(Exception exception)
        {
            var details = new ExceptionDetails
            {
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty
            };

            // Get inner exceptions
            var inner = exception.InnerException;
            while (inner != null)
            {
                details.InnerExceptions.Add(new InnerExceptionInfo
                {
                    ExceptionType = inner.GetType().FullName ?? inner.GetType().Name,
                    Message = inner.Message
                });
                inner = inner.InnerException;
            }

            // Get exception data
            foreach (DictionaryEntry entry in exception.Data)
            {
                var key = entry.Key?.ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    details.Data[key] = entry.Value ?? "null";
                }
            }

            return details;
        }

        private PerformanceCounters GetPerformanceCounters()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                
                return new PerformanceCounters
                {
                    WorkingSetMB = process.WorkingSet64 / (1024 * 1024),
                    PrivateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024),
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    TotalProcessorTimeMS = (long)process.TotalProcessorTime.TotalMilliseconds,
                    StartTime = process.StartTime,
                    Uptime = DateTime.Now - process.StartTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error gathering performance counters");
                return new PerformanceCounters();
            }
        }

        private async Task<NetworkDiagnostics> GetNetworkDiagnosticsAsync()
        {
            try
            {
                var networkInfo = new NetworkDiagnostics
                {
                    IsNetworkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()
                };

                // Get network interfaces
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                networkInfo.NetworkInterfaces = interfaces
                    .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    .Select(ni => $"{ni.Name} ({ni.NetworkInterfaceType})")
                    .ToList();

                // Test basic connectivity to common endpoints
                var testEndpoints = new[]
                {
                    "8.8.8.8", // Google DNS
                    "1.1.1.1", // Cloudflare DNS
                    "www.microsoft.com"
                };

                foreach (var endpoint in testEndpoints)
                {
                    try
                    {
                        using var ping = new System.Net.NetworkInformation.Ping();
                        var reply = await ping.SendPingAsync(endpoint, 5000);
                        networkInfo.EndpointConnectivity[endpoint] = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                    }
                    catch
                    {
                        networkInfo.EndpointConnectivity[endpoint] = false;
                    }
                }

                return networkInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error gathering network diagnostics");
                return new NetworkDiagnostics();
            }
        }
    }
}