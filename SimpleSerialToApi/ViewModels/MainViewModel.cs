using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly SerialCommunicationService _serialService;
        private readonly SimpleQueueService _queueService;
        private readonly SimpleHttpService _httpService;
        private readonly TimerService _timerService;

        private string _serialPort = "COM1";
        private string _apiUrl = "http://localhost:8080/api/data";
        private bool _isConnected = false;
        private bool _isTimerRunning = false;
        private int _queueCount = 0;
        private string _status = "Disconnected";

        public MainViewModel(
            ILogger<MainViewModel> logger,
            SerialCommunicationService serialService,
            SimpleQueueService queueService,
            SimpleHttpService httpService,
            TimerService timerService)
        {
            _logger = logger;
            _serialService = serialService;
            _queueService = queueService;
            _httpService = httpService;
            _timerService = timerService;

            // Commands
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);
            StartTimerCommand = new RelayCommand(StartTimer, CanStartTimer);
            StopTimerCommand = new RelayCommand(StopTimer, CanStopTimer);
            TestApiCommand = new RelayCommand(TestApi);

            // 이벤트 구독
            _serialService.DataReceived += OnSerialDataReceived;
            _serialService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // API URL 설정
            _httpService.SetApiUrl(_apiUrl);
        }

        // Properties
        public string SerialPort
        {
            get => _serialPort;
            set { _serialPort = value; OnPropertyChanged(); }
        }

        public string ApiUrl
        {
            get => _apiUrl;
            set { _apiUrl = value; OnPropertyChanged(); _httpService.SetApiUrl(value); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            set { _isTimerRunning = value; OnPropertyChanged(); }
        }

        public int QueueCount
        {
            get => _queueCount;
            set { _queueCount = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand StartTimerCommand { get; }
        public ICommand StopTimerCommand { get; }
        public ICommand TestApiCommand { get; }

        // Command Methods
        private async void Connect()
        {
            try
            {
                Status = "Connecting...";
                var success = await _serialService.ConnectAsync();
                IsConnected = success;
                Status = success ? "Connected" : "Connection Failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to serial port");
                Status = "Connection Error";
            }
        }

        private async void Disconnect()
        {
            try
            {
                await _serialService.DisconnectAsync();
                IsConnected = false;
                Status = "Disconnected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from serial port");
            }
        }

        private void StartTimer()
        {
            _timerService.Start(5); // 5초 간격
            IsTimerRunning = true;
            Status = "Timer Started";
        }

        private void StopTimer()
        {
            _timerService.Stop();
            IsTimerRunning = false;
            Status = "Timer Stopped";
        }

        private async void TestApi()
        {
            try
            {
                Status = "Testing API...";
                var success = await _httpService.TestConnectionAsync();
                Status = success ? "API Connection OK" : "API Connection Failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                Status = "API Test Error";
            }
        }

        // Command Can Execute
        private bool CanConnect() => !IsConnected;
        private bool CanDisconnect() => IsConnected;
        private bool CanStartTimer() => !IsTimerRunning;
        private bool CanStopTimer() => IsTimerRunning;

        // Event Handlers
        private void OnSerialDataReceived(object? sender, Models.SerialDataReceivedEventArgs e)
        {
            // STX/ETX 기반 파싱하여 큐에 추가
            _queueService.ParseAndEnqueue(e.Data);
            QueueCount = _queueService.Count;
            Status = $"Data received. Queue: {QueueCount}";
        }

        private void OnConnectionStatusChanged(object? sender, Models.SerialConnectionEventArgs e)
        {
            IsConnected = e.IsConnected;
            Status = e.Message;
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
