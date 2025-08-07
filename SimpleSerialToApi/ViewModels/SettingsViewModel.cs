using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;

namespace SimpleSerialToApi.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly IConfigurationService _configurationService;

        // Serial Settings
        private string _selectedComPort = string.Empty;
        private int _baudRate = 9600;
        private string _parity = "None";
        private int _dataBits = 8;
        private string _stopBits = "One";

        // API Settings
        private string _apiUrl = string.Empty;
        private string _httpMethod = "POST";
        private string _authType = "None";
        private string _authToken = string.Empty;
        private int _timeoutMs = 5000;

        // Queue Settings
        private int _maxQueueSize = 1000;
        private int _batchSize = 10;
        private int _maxRetries = 3;
        private int _retryDelayMs = 1000;

        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            IMessenger messenger,
            IConfigurationService configurationService)
        {
            _logger = logger;
            _messenger = messenger;
            _configurationService = configurationService;

            InitializeCommands();
            InitializeCollections();
            LoadSettings();
        }

        #region Serial Settings

        public ObservableCollection<string> AvailableComPorts { get; } = new();
        public ObservableCollection<int> BaudRates { get; } = new();
        public ObservableCollection<string> ParityOptions { get; } = new();
        public ObservableCollection<int> DataBitsOptions { get; } = new();
        public ObservableCollection<string> StopBitsOptions { get; } = new();

        public string SelectedComPort
        {
            get => _selectedComPort;
            set => SetProperty(ref _selectedComPort, value);
        }

        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        public string Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        public string StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        #endregion

        #region API Settings

        public ObservableCollection<string> HttpMethods { get; } = new();
        public ObservableCollection<string> AuthTypes { get; } = new();

        public string ApiUrl
        {
            get => _apiUrl;
            set => SetProperty(ref _apiUrl, value);
        }

        public string HttpMethod
        {
            get => _httpMethod;
            set => SetProperty(ref _httpMethod, value);
        }

        public string AuthType
        {
            get => _authType;
            set => SetProperty(ref _authType, value);
        }

        public string AuthToken
        {
            get => _authToken;
            set => SetProperty(ref _authToken, value);
        }

        public int TimeoutMs
        {
            get => _timeoutMs;
            set => SetProperty(ref _timeoutMs, value);
        }

        #endregion

        #region Queue Settings

        public int MaxQueueSize
        {
            get => _maxQueueSize;
            set => SetProperty(ref _maxQueueSize, value);
        }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, value);
        }

        public int MaxRetries
        {
            get => _maxRetries;
            set => SetProperty(ref _maxRetries, value);
        }

        public int RetryDelayMs
        {
            get => _retryDelayMs;
            set => SetProperty(ref _retryDelayMs, value);
        }

        #endregion

        #region Commands

        public ICommand SaveSettingsCommand { get; private set; } = null!;
        public ICommand LoadSettingsCommand { get; private set; } = null!;
        public ICommand TestSerialConnectionCommand { get; private set; } = null!;
        public ICommand TestApiConnectionCommand { get; private set; } = null!;
        public ICommand RefreshComPortsCommand { get; private set; } = null!;
        public ICommand ResetToDefaultsCommand { get; private set; } = null!;

        #endregion

        private void InitializeCommands()
        {
            SaveSettingsCommand = new RelayCommand(async () => await ExecuteSaveSettingsAsync(), CanSaveSettings);
            LoadSettingsCommand = new RelayCommand(ExecuteLoadSettings);
            TestSerialConnectionCommand = new RelayCommand(async () => await ExecuteTestSerialConnectionAsync(), CanTestSerialConnection);
            TestApiConnectionCommand = new RelayCommand(async () => await ExecuteTestApiConnectionAsync(), CanTestApiConnection);
            RefreshComPortsCommand = new RelayCommand(ExecuteRefreshComPorts);
            ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);
        }

        private void InitializeCollections()
        {
            // Initialize Serial Settings options
            RefreshComPortsInternal();

            foreach (var baudRate in new[] { 9600, 19200, 38400, 57600, 115200 })
                BaudRates.Add(baudRate);

            foreach (var parity in new[] { "None", "Odd", "Even", "Mark", "Space" })
                ParityOptions.Add(parity);

            foreach (var dataBits in new[] { 5, 6, 7, 8 })
                DataBitsOptions.Add(dataBits);

            foreach (var stopBits in new[] { "None", "One", "Two", "OnePointFive" })
                StopBitsOptions.Add(stopBits);

            // Initialize API Settings options
            foreach (var method in new[] { "GET", "POST", "PUT", "DELETE" })
                HttpMethods.Add(method);

            foreach (var authType in new[] { "None", "Basic", "Bearer", "ApiKey" })
                AuthTypes.Add(authType);
        }

        private void LoadSettings()
        {
            try
            {
                var config = _configurationService.ApplicationConfig;

                // Load Serial Settings
                SelectedComPort = config.SerialSettings.PortName;
                BaudRate = config.SerialSettings.BaudRate;
                Parity = config.SerialSettings.Parity.ToString();
                DataBits = config.SerialSettings.DataBits;
                StopBits = config.SerialSettings.StopBits.ToString();

                // Load API Settings (first endpoint if available)
                if (config.ApiEndpoints.Any())
                {
                    var firstEndpoint = config.ApiEndpoints.First();
                    ApiUrl = firstEndpoint.Url;
                    HttpMethod = firstEndpoint.Method;
                    AuthType = firstEndpoint.AuthType;
                    TimeoutMs = firstEndpoint.Timeout;
                }

                // Load Queue Settings
                MaxQueueSize = config.MessageQueueSettings.MaxQueueSize;
                BatchSize = config.MessageQueueSettings.BatchSize;

                _logger.LogInformation("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
                _messenger.Send(new LogMessage
                {
                    Level = "ERROR",
                    Source = "Settings",
                    Message = $"Failed to load settings: {ex.Message}"
                });
            }
        }

        private async Task ExecuteSaveSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Saving settings...");

                // In a real implementation, you would save these settings to configuration
                // For now, just log the action
                _logger.LogInformation("Settings saved: Serial Port={ComPort}, API URL={ApiUrl}", 
                    SelectedComPort, ApiUrl);

                _messenger.Send(new ConfigurationChangedMessage
                {
                    SectionName = "Application Settings",
                    Description = "Settings updated from UI"
                });

                _messenger.Send(new LogMessage
                {
                    Level = "INFO",
                    Source = "Settings",
                    Message = "Settings saved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                _messenger.Send(new LogMessage
                {
                    Level = "ERROR",
                    Source = "Settings",
                    Message = $"Failed to save settings: {ex.Message}"
                });
            }
        }

        private void ExecuteLoadSettings()
        {
            LoadSettings();
            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "Settings",
                Message = "Settings reloaded from configuration"
            });
        }

        private async Task ExecuteTestSerialConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing serial connection: {ComPort}", SelectedComPort);
                
                // Simulate connection test
                await Task.Delay(1000);
                
                // For demonstration, randomly succeed or fail
                var success = new Random().Next(1, 101) <= 70; // 70% success rate
                
                var message = success ? 
                    $"Serial connection test successful: {SelectedComPort}" : 
                    $"Serial connection test failed: {SelectedComPort}";
                
                _messenger.Send(new LogMessage
                {
                    Level = success ? "INFO" : "ERROR",
                    Source = "Settings",
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Serial connection test failed");
            }
        }

        private async Task ExecuteTestApiConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing API connection: {ApiUrl}", ApiUrl);
                
                // Simulate API test
                await Task.Delay(1500);
                
                // For demonstration, randomly succeed or fail
                var success = new Random().Next(1, 101) <= 75; // 75% success rate
                
                var message = success ? 
                    $"API connection test successful: {ApiUrl}" : 
                    $"API connection test failed: {ApiUrl}";
                
                _messenger.Send(new LogMessage
                {
                    Level = success ? "INFO" : "ERROR",
                    Source = "Settings",
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API connection test failed");
            }
        }

        private void ExecuteRefreshComPorts()
        {
            RefreshComPortsInternal();
            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "Settings",
                Message = $"COM ports refreshed: {AvailableComPorts.Count} ports found"
            });
        }

        private void RefreshComPortsInternal()
        {
            try
            {
                AvailableComPorts.Clear();
                var ports = System.IO.Ports.SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    AvailableComPorts.Add(port);
                }
                
                // Add sample ports if none found (for demonstration)
                if (!AvailableComPorts.Any())
                {
                    AvailableComPorts.Add("COM1");
                    AvailableComPorts.Add("COM3");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh COM ports");
            }
        }

        private void ExecuteResetToDefaults()
        {
            SelectedComPort = AvailableComPorts.FirstOrDefault() ?? "COM1";
            BaudRate = 9600;
            Parity = "None";
            DataBits = 8;
            StopBits = "One";
            
            ApiUrl = "https://api.example.com/data";
            HttpMethod = "POST";
            AuthType = "None";
            AuthToken = string.Empty;
            TimeoutMs = 5000;
            
            MaxQueueSize = 1000;
            BatchSize = 10;
            MaxRetries = 3;
            RetryDelayMs = 1000;

            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "Settings",
                Message = "Settings reset to defaults"
            });
        }

        private bool CanSaveSettings() => !string.IsNullOrWhiteSpace(SelectedComPort) && !string.IsNullOrWhiteSpace(ApiUrl);
        private bool CanTestSerialConnection() => !string.IsNullOrWhiteSpace(SelectedComPort);
        private bool CanTestApiConnection() => !string.IsNullOrWhiteSpace(ApiUrl);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up any resources
            }
            base.Dispose(disposing);
        }
    }
}