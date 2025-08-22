using Microsoft.Extensions.Logging;
using System.Configuration;
using System.IO.Ports;
using System.Management;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Service for discovering and managing COM port selection
    /// </summary>
    public class ComPortDiscoveryService
    {
        private readonly ILogger<ComPortDiscoveryService> _logger;
        private const string LAST_USED_PORT_KEY = "LastUsedComPort";

        public ComPortDiscoveryService(ILogger<ComPortDiscoveryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the best available COM port using smart selection logic
        /// </summary>
        /// <returns>COM port name or null if none available</returns>
        public string? GetBestAvailableComPort()
        {
            try
            {
                // 1순위: 마지막 사용 포트가 여전히 사용 가능한지 확인
                var lastUsedPort = GetLastUsedComPort();
                if (!string.IsNullOrEmpty(lastUsedPort) && IsPortAvailable(lastUsedPort))
                {
                    _logger.LogInformation("Using last used COM port: {Port}", lastUsedPort);
                    return lastUsedPort;
                }

                // 2순위: "Serial" 또는 "USB" 포함된 포트 검색 (내림차순)
                var smartSelectedPort = GetSmartSelectedPort();
                if (!string.IsNullOrEmpty(smartSelectedPort))
                {
                    _logger.LogInformation("Smart selected COM port: {Port}", smartSelectedPort);
                    return smartSelectedPort;
                }

                // 3순위: 사용 가능한 첫 번째 포트
                var availablePorts = SerialPort.GetPortNames();
                var firstAvailable = availablePorts.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstAvailable))
                {
                    _logger.LogInformation("Using first available COM port: {Port}", firstAvailable);
                    return firstAvailable;
                }

                _logger.LogWarning("No COM ports available");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering COM ports");
                return null;
            }
        }

        /// <summary>
        /// Gets all available COM ports with their descriptions
        /// </summary>
        /// <returns>Dictionary of port names and descriptions</returns>
        public Dictionary<string, string> GetAvailablePortsWithDescriptions()
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                var availablePorts = SerialPort.GetPortNames();
                
                foreach (var port in availablePorts)
                {
                    var description = GetPortDescription(port);
                    result[port] = description;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting port descriptions");
            }

            return result;
        }

        /// <summary>
        /// Saves the last used COM port to configuration
        /// </summary>
        /// <param name="portName">COM port name</param>
        public void SaveLastUsedComPort(string portName)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings[LAST_USED_PORT_KEY] != null)
                {
                    config.AppSettings.Settings[LAST_USED_PORT_KEY].Value = portName;
                }
                else
                {
                    config.AppSettings.Settings.Add(LAST_USED_PORT_KEY, portName);
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                _logger.LogInformation("Saved last used COM port: {Port}", portName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving last used COM port: {Port}", portName);
            }
        }

        /// <summary>
        /// Enables auto-connect for successful connection
        /// </summary>
        /// <param name="portName">COM port name</param>
        public void EnableAutoConnect(string portName)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // Set AutoConnectEnabled
                if (config.AppSettings.Settings["AutoConnectEnabled"] != null)
                {
                    config.AppSettings.Settings["AutoConnectEnabled"].Value = "true";
                }
                else
                {
                    config.AppSettings.Settings.Add("AutoConnectEnabled", "true");
                }
                
                // Set AutoConnectPort
                if (config.AppSettings.Settings["AutoConnectPort"] != null)
                {
                    config.AppSettings.Settings["AutoConnectPort"].Value = portName;
                }
                else
                {
                    config.AppSettings.Settings.Add("AutoConnectPort", portName);
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                _logger.LogInformation("Auto-connect enabled for port: {Port}", portName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling auto-connect for port: {Port}", portName);
            }
        }

        /// <summary>
        /// Gets auto-connect settings
        /// </summary>
        /// <returns>Tuple of (enabled, portName)</returns>
        public (bool enabled, string? portName) GetAutoConnectSettings()
        {
            try
            {
                var enabled = bool.TryParse(ConfigurationManager.AppSettings["AutoConnectEnabled"], out var autoConnectEnabled) && autoConnectEnabled;
                var portName = ConfigurationManager.AppSettings["AutoConnectPort"];
                
                return (enabled, portName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading auto-connect settings");
                return (false, null);
            }
        }

        /// <summary>
        /// Gets the last used COM port from configuration
        /// </summary>
        /// <returns>Last used COM port name or null</returns>
        private string? GetLastUsedComPort()
        {
            try
            {
                return ConfigurationManager.AppSettings[LAST_USED_PORT_KEY];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading last used COM port");
                return null;
            }
        }

        /// <summary>
        /// Checks if a COM port is currently available
        /// </summary>
        /// <param name="portName">COM port name</param>
        /// <returns>True if available, false otherwise</returns>
        private bool IsPortAvailable(string portName)
        {
            try
            {
                var availablePorts = SerialPort.GetPortNames();
                return availablePorts.Contains(portName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking port availability: {Port}", portName);
                return false;
            }
        }

        /// <summary>
        /// Gets smart selected port based on description containing "Serial" or "USB"
        /// </summary>
        /// <returns>Smart selected COM port or null</returns>
        private string? GetSmartSelectedPort()
        {
            try
            {
                var availablePorts = SerialPort.GetPortNames();
                var smartPorts = new List<(string Port, string Description)>();

                foreach (var port in availablePorts)
                {
                    var description = GetPortDescription(port);
                    if (description.Contains("Serial", StringComparison.OrdinalIgnoreCase) ||
                        description.Contains("USB", StringComparison.OrdinalIgnoreCase))
                    {
                        smartPorts.Add((port, description));
                        _logger.LogDebug("Found smart port candidate: {Port} - {Description}", port, description);
                    }
                }

                // 내림차순 정렬 (COM10, COM9, COM8, ... COM3, COM2, COM1)
                var selectedPort = smartPorts
                    .OrderByDescending(p => p.Port, new ComPortComparer())
                    .FirstOrDefault();

                return selectedPort.Port;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart port selection");
                return null;
            }
        }

        /// <summary>
        /// Gets COM port description using WMI
        /// </summary>
        /// <param name="portName">COM port name</param>
        /// <returns>Port description or port name if description not found</returns>
        private string GetPortDescription(string portName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");
                
                foreach (ManagementObject obj in searcher.Get())
                {
                    var caption = obj["Caption"]?.ToString() ?? "";
                    if (caption.Contains($"({portName})", StringComparison.OrdinalIgnoreCase))
                    {
                        return caption;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get description for port {Port}", portName);
            }

            return portName; // Fallback to port name
        }
    }

    /// <summary>
    /// Custom comparer for COM port names to handle natural sorting (COM1, COM2, ... COM10, COM11)
    /// </summary>
    public class ComPortComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null || y == null) return 0;

            // Extract numbers from COM port names
            var xNumber = ExtractPortNumber(x);
            var yNumber = ExtractPortNumber(y);

            return xNumber.CompareTo(yNumber);
        }

        private int ExtractPortNumber(string portName)
        {
            if (portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                var numberStr = portName.Substring(3);
                if (int.TryParse(numberStr, out int number))
                {
                    return number;
                }
            }
            return 0;
        }
    }
}
