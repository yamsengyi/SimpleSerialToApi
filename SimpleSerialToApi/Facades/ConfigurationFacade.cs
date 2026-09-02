using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 앱 설정 관리를 위한 파사드 구현
    /// </summary>
    public class ConfigurationFacade : IConfigurationFacade, INotifyPropertyChanged
    {
        private readonly ILogger<ConfigurationFacade> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly SerialCommunicationService _serialService;

        private string _apiUrl = "http://localhost:8080/api/data";
        private string _transmissionInterval = "5";
        private string _batchSize = "10";
        private string _deviceId = string.Empty;

        public ConfigurationFacade(
            ILogger<ConfigurationFacade> logger,
            IConfigurationService configurationService,
            SerialCommunicationService serialService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _serialService = serialService;
        }

        public string ApiUrl
        {
            get => _apiUrl;
            set { _apiUrl = value; OnPropertyChanged(); }
        }

        public string TransmissionInterval
        {
            get => _transmissionInterval;
            set { _transmissionInterval = value; OnPropertyChanged(); }
        }

        public string BatchSize
        {
            get => _batchSize;
            set { _batchSize = value; OnPropertyChanged(); }
        }

        public string DeviceId
        {
            get => _deviceId;
            set { _deviceId = value; OnPropertyChanged(); }
        }

        public void LoadApiUrl()
        {
            try
            {
                var config = _configurationService.ApplicationConfig;
                if (config.ApiEndpoints != null && config.ApiEndpoints.Any())
                {
                    var defaultEndpoint = config.ApiEndpoints.FirstOrDefault(e =>
                        e.Name.Equals("default", StringComparison.OrdinalIgnoreCase))
                        ?? config.ApiEndpoints.First();
                    _apiUrl = defaultEndpoint.Url;
                    return;
                }

                var legacyApiUrl = System.Configuration.ConfigurationManager.AppSettings["ApiEndpoint"];
                if (!string.IsNullOrEmpty(legacyApiUrl))
                {
                    _apiUrl = legacyApiUrl;
                    return;
                }

                _logger.LogWarning("No API URL found in configuration, using default: {ApiUrl}", _apiUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading API URL from configuration, using default: {ApiUrl}", _apiUrl);
            }
        }

        public void LoadQueueSettings()
        {
            try
            {
                var transmissionInterval = System.Configuration.ConfigurationManager.AppSettings["QueueTransmissionInterval"] ?? "5";
                var batchSize = System.Configuration.ConfigurationManager.AppSettings["QueueBatchSize"] ?? "10";
                var deviceId = System.Configuration.ConfigurationManager.AppSettings["DeviceId"] ?? "";

                TransmissionInterval = transmissionInterval;
                BatchSize = batchSize;
                DeviceId = deviceId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from App.config");
                TransmissionInterval = "5";
                BatchSize = "10";
                DeviceId = "";
            }
        }

        public void SetTransmissionInterval()
        {
            try
            {
                if (int.TryParse(TransmissionInterval, out int interval) && interval > 0)
                {
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                        System.Configuration.ConfigurationUserLevel.None);
                    config.AppSettings.Settings["QueueTransmissionInterval"].Value = TransmissionInterval;
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                    _logger.LogInformation("Transmission interval set to {Interval} seconds", interval);
                }
                else
                {
                    _logger.LogWarning("Invalid transmission interval entered: {Input}", TransmissionInterval);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting transmission interval");
            }
        }

        public void SetBatchSize()
        {
            try
            {
                if (int.TryParse(BatchSize, out int batchSize) && batchSize > 0)
                {
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                        System.Configuration.ConfigurationUserLevel.None);
                    config.AppSettings.Settings["QueueBatchSize"].Value = BatchSize;
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                    _logger.LogInformation("Batch size set to {BatchSize}", batchSize);
                }
                else
                {
                    _logger.LogWarning("Invalid batch size entered: {Input}", BatchSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting batch size");
            }
        }

        public void SetDeviceId()
        {
            try
            {
                var normalizedDeviceId = DeviceId?.Trim() ?? string.Empty;

                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                    System.Configuration.ConfigurationUserLevel.None);

                var settings = config.AppSettings.Settings;
                if (settings["DeviceId"] == null)
                {
                    settings.Add("DeviceId", normalizedDeviceId);
                }
                else
                {
                    settings["DeviceId"].Value = normalizedDeviceId;
                }

                config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                _logger.LogInformation(
                    normalizedDeviceId.Length == 0
                        ? "Device ID cleared from configuration"
                        : "Device ID set to '{DeviceId}'",
                    normalizedDeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Device ID");
            }
        }

        public async void OpenSerialConfig()
        {
            try
            {
                var currentSettings = _serialService.ConnectionSettings;
                var window = new Views.SerialConfigWindow(currentSettings);
                var result = window.ShowDialog();

                if (result == true && window.IsChanged)
                {
                    _configurationService.SaveSerialSettings(window.Settings);
                    _serialService.UpdateConnectionSettings(window.Settings);
                    _logger.LogInformation("Serial configuration updated and saved");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening serial configuration window");
                System.Windows.MessageBox.Show($"Error opening serial configuration: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
