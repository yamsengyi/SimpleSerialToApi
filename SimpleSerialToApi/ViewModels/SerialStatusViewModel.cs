using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;

namespace SimpleSerialToApi.ViewModels
{
    public class SerialStatusViewModel : ViewModelBase
    {
        private readonly ISerialCommunicationService _serialService;
        private readonly ILogger<SerialStatusViewModel> _logger;
        private readonly IMessenger _messenger;

        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        private string _portName = string.Empty;
        private string _baudRate = string.Empty;
        private DateTime _lastDataReceived = DateTime.MinValue;
        private int _totalMessagesReceived = 0;
        private string _statusMessage = "Disconnected";

        public SerialStatusViewModel(
            ISerialCommunicationService serialService,
            ILogger<SerialStatusViewModel> logger,
            IMessenger messenger)
        {
            _serialService = serialService;
            _logger = logger;
            _messenger = messenger;

            InitializeCommands();
            InitializeFromService();
            SubscribeToServiceEvents();

            AvailablePorts = new ObservableCollection<string>(_serialService.GetAvailablePorts());
        }

        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string PortName
        {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }

        public string BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        public DateTime LastDataReceived
        {
            get => _lastDataReceived;
            set => SetProperty(ref _lastDataReceived, value);
        }

        public int TotalMessagesReceived
        {
            get => _totalMessagesReceived;
            set => SetProperty(ref _totalMessagesReceived, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> AvailablePorts { get; }

        public ICommand ConnectCommand { get; private set; } = null!;
        public ICommand DisconnectCommand { get; private set; } = null!;
        public ICommand SendTestCommand { get; private set; } = null!;
        public ICommand RefreshPortsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            ConnectCommand = new RelayCommand(async () => await ExecuteConnectAsync(), CanConnect);
            DisconnectCommand = new RelayCommand(async () => await ExecuteDisconnectAsync(), CanDisconnect);
            SendTestCommand = new RelayCommand(async () => await ExecuteSendTestAsync(), CanSendTest);
            RefreshPortsCommand = new RelayCommand(ExecuteRefreshPorts);
        }

        private void InitializeFromService()
        {
            var settings = _serialService.ConnectionSettings;
            PortName = settings.PortName;
            BaudRate = settings.BaudRate.ToString();
            ConnectionStatus = _serialService.IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
            UpdateStatusMessage();
        }

        private void SubscribeToServiceEvents()
        {
            _serialService.DataReceived += OnDataReceived;
            _serialService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void OnDataReceived(object? sender, SimpleSerialToApi.Models.SerialDataReceivedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                TotalMessagesReceived++;
                LastDataReceived = DateTime.Now;
                
                _messenger.Send(new LogMessage
                {
                    Level = "INFO",
                    Source = "Serial",
                    Message = $"Data received: {e.DataAsText}"
                });
            });
        }

    private void OnConnectionStatusChanged(object? sender, SimpleSerialToApi.Models.SerialConnectionEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ConnectionStatus = e.IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                StatusMessage = e.Message;
                
                _messenger.Send(new StatusUpdatedMessage
                {
                    ComponentName = "Serial",
                    Status = ConnectionStatus.ToString()
                });
            });
        }

        private async Task ExecuteConnectAsync()
        {
            try
            {
                ConnectionStatus = ConnectionStatus.Connecting;
                StatusMessage = "Connecting...";
                
                var success = await _serialService.ConnectAsync();
                if (success)
                {
                }
                else
                {
                    ConnectionStatus = ConnectionStatus.Error;
                    StatusMessage = "Connection failed";
                    _logger.LogError("Failed to connect to serial port {PortName}", PortName);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionStatus.Error;
                StatusMessage = $"Connection error: {ex.Message}";
                _logger.LogError(ex, "Error connecting to serial port");
            }
        }

        private async Task ExecuteDisconnectAsync()
        {
            try
            {
                await _serialService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from serial port");
            }
        }

        private async Task ExecuteSendTestAsync()
        {
            try
            {
                var success = await _serialService.SendTextAsync("TEST\r\n");
                var message = success ? "Test message sent successfully" : "Failed to send test message";
                
                _messenger.Send(new LogMessage
                {
                    Level = success ? "INFO" : "ERROR",
                    Source = "Serial",
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test message");
            }
        }

        private void ExecuteRefreshPorts()
        {
            try
            {
                AvailablePorts.Clear();
                foreach (var port in _serialService.GetAvailablePorts())
                {
                    AvailablePorts.Add(port);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing ports");
            }
        }

        private bool CanConnect() => ConnectionStatus == ConnectionStatus.Disconnected && !string.IsNullOrEmpty(PortName);
        private bool CanDisconnect() => ConnectionStatus == ConnectionStatus.Connected;
        private bool CanSendTest() => ConnectionStatus == ConnectionStatus.Connected;

        private void UpdateStatusMessage()
        {
            StatusMessage = ConnectionStatus switch
            {
                ConnectionStatus.Connected => $"Connected to {PortName}",
                ConnectionStatus.Connecting => "Connecting...",
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Error => "Connection Error",
                _ => "Unknown"
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _serialService.DataReceived -= OnDataReceived;
                _serialService.ConnectionStatusChanged -= OnConnectionStatusChanged;
            }
            base.Dispose(disposing);
        }
    }
}