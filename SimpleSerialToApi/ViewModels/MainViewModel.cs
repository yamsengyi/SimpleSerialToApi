using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;

namespace SimpleSerialToApi.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly IServiceProvider _serviceProvider;

        private ApplicationState _applicationState = ApplicationState.Stopped;
        private string _applicationStatus = "Ready";
        private bool _isApplicationRunning = false;
        private PerformanceMetrics _performanceMetrics = new();

        public MainViewModel(
            ILogger<MainViewModel> logger,
            IMessenger messenger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _messenger = messenger;
            _serviceProvider = serviceProvider;

            InitializeChildViewModels();
            InitializeCommands();
            SubscribeToMessages();

            Logs = new ObservableCollection<LogEntry>();
        }

        public SerialStatusViewModel SerialStatus { get; private set; } = null!;
        public ApiStatusViewModel ApiStatus { get; private set; } = null!;
        public QueueStatusViewModel QueueStatus { get; private set; } = null!;
        public SettingsViewModel Settings { get; private set; } = null!;

        public ApplicationState ApplicationState
        {
            get => _applicationState;
            set => SetProperty(ref _applicationState, value);
        }

        public string ApplicationStatus
        {
            get => _applicationStatus;
            set => SetProperty(ref _applicationStatus, value);
        }

        public bool IsApplicationRunning
        {
            get => _isApplicationRunning;
            set => SetProperty(ref _isApplicationRunning, value);
        }

        public PerformanceMetrics PerformanceMetrics
        {
            get => _performanceMetrics;
            set => SetProperty(ref _performanceMetrics, value);
        }

        public ObservableCollection<LogEntry> Logs { get; }

        public ICommand StartApplicationCommand { get; private set; } = null!;
        public ICommand StopApplicationCommand { get; private set; } = null!;
        public ICommand OpenSettingsCommand { get; private set; } = null!;
        public ICommand ClearLogsCommand { get; private set; } = null!;
        public ICommand ExportLogsCommand { get; private set; } = null!;

        private void InitializeChildViewModels()
        {
            SerialStatus = (SerialStatusViewModel)_serviceProvider.GetService(typeof(SerialStatusViewModel))!;
            ApiStatus = (ApiStatusViewModel)_serviceProvider.GetService(typeof(ApiStatusViewModel))!;
            QueueStatus = (QueueStatusViewModel)_serviceProvider.GetService(typeof(QueueStatusViewModel))!;
            Settings = (SettingsViewModel)_serviceProvider.GetService(typeof(SettingsViewModel))!;
        }

        private void InitializeCommands()
        {
            StartApplicationCommand = new RelayCommand(async () => await ExecuteStartApplicationAsync(), CanStartApplication);
            StopApplicationCommand = new RelayCommand(async () => await ExecuteStopApplicationAsync(), CanStopApplication);
            OpenSettingsCommand = new RelayCommand(ExecuteOpenSettings);
            ClearLogsCommand = new RelayCommand(ExecuteClearLogs);
            ExportLogsCommand = new RelayCommand(async () => await ExecuteExportLogsAsync());
        }

        private void SubscribeToMessages()
        {
            _messenger.Subscribe<StatusUpdatedMessage>(OnStatusUpdated);
            _messenger.Subscribe<LogMessage>(OnLogReceived);
            _messenger.Subscribe<ConfigurationChangedMessage>(OnConfigurationChanged);
        }

        private void OnStatusUpdated(StatusUpdatedMessage message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Update application status based on component status changes
                UpdateApplicationStatus();
            });
        }

        private void OnLogReceived(LogMessage message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var logEntry = new LogEntry
                {
                    Timestamp = message.Timestamp,
                    Level = message.Level,
                    Source = message.Source ?? "Unknown",
                    Message = message.Message
                };

                Logs.Insert(0, logEntry); // Add to top

                // Keep only the last 1000 log entries to prevent memory issues
                while (Logs.Count > 1000)
                {
                    Logs.RemoveAt(Logs.Count - 1);
                }
            });
        }

        private void OnConfigurationChanged(ConfigurationChangedMessage message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = "INFO",
                    Source = "Configuration",
                    Message = $"Configuration changed: {message.SectionName} - {message.Description}"
                };

                Logs.Insert(0, logEntry);
            });
        }

        private async Task ExecuteStartApplicationAsync()
        {
            try
            {
                _logger.LogInformation("Starting application...");
                ApplicationState = ApplicationState.Starting;
                ApplicationStatus = "Starting services...";

                // Start serial communication
                if (!SerialStatus.ConnectionStatus == ConnectionStatus.Connected)
                {
                    if (SerialStatus.ConnectCommand.CanExecute(null))
                    {
                        SerialStatus.ConnectCommand.Execute(null);
                        await Task.Delay(1000); // Give time for connection
                    }
                }

                ApplicationState = ApplicationState.Running;
                ApplicationStatus = "Running";
                IsApplicationRunning = true;

                _logger.LogInformation("Application started successfully");
                
                // Start performance monitoring
                StartPerformanceMonitoring();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start application");
                ApplicationState = ApplicationState.Error;
                ApplicationStatus = $"Start failed: {ex.Message}";
            }
        }

        private async Task ExecuteStopApplicationAsync()
        {
            try
            {
                _logger.LogInformation("Stopping application...");
                ApplicationState = ApplicationState.Stopping;
                ApplicationStatus = "Stopping services...";

                // Stop serial communication
                if (SerialStatus.ConnectionStatus == ConnectionStatus.Connected)
                {
                    if (SerialStatus.DisconnectCommand.CanExecute(null))
                    {
                        SerialStatus.DisconnectCommand.Execute(null);
                        await Task.Delay(1000); // Give time for disconnection
                    }
                }

                ApplicationState = ApplicationState.Stopped;
                ApplicationStatus = "Stopped";
                IsApplicationRunning = false;

                _logger.LogInformation("Application stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop application");
                ApplicationState = ApplicationState.Error;
                ApplicationStatus = $"Stop failed: {ex.Message}";
            }
        }

        private void ExecuteOpenSettings()
        {
            // This could open a settings dialog or switch to a settings tab
            _logger.LogInformation("Opening settings");
            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "UI",
                Message = "Settings dialog opened"
            });
        }

        private void ExecuteClearLogs()
        {
            Logs.Clear();
            _logger.LogInformation("Log entries cleared");
        }

        private async Task ExecuteExportLogsAsync()
        {
            try
            {
                var fileName = $"SimpleSerialToApi_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var content = string.Join(Environment.NewLine, 
                    Logs.Select(l => $"{l.Timestamp:yyyy-MM-dd HH:mm:ss} [{l.Level}] {l.Source}: {l.Message}"));

                // In a real implementation, you would save this to a file
                // For now, just log the action
                _logger.LogInformation("Exported {LogCount} log entries to {FileName}", Logs.Count, fileName);
                
                _messenger.Send(new LogMessage
                {
                    Level = "INFO",
                    Source = "UI",
                    Message = $"Exported {Logs.Count} log entries to {fileName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export logs");
            }
        }

        private bool CanStartApplication() => ApplicationState == ApplicationState.Stopped;
        private bool CanStopApplication() => ApplicationState == ApplicationState.Running;

        private void UpdateApplicationStatus()
        {
            if (!IsApplicationRunning) return;

            var serialOk = SerialStatus.ConnectionStatus == ConnectionStatus.Connected;
            var apiOk = ApiStatus.OverallStatus == ConnectionStatus.Connected;

            ApplicationStatus = (serialOk, apiOk) switch
            {
                (true, true) => "All systems operational",
                (true, false) => "Serial connected, API issues",
                (false, true) => "API connected, Serial issues",
                (false, false) => "Connection issues detected",
            };
        }

        private void StartPerformanceMonitoring()
        {
            // This would typically start a timer to update performance metrics
            // For now, we'll just initialize with sample data
            PerformanceMetrics = new PerformanceMetrics
            {
                MessagesReceived = SerialStatus.TotalMessagesReceived,
                LastUpdate = DateTime.Now
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messenger.Unsubscribe<StatusUpdatedMessage>(OnStatusUpdated);
                _messenger.Unsubscribe<LogMessage>(OnLogReceived);
                _messenger.Unsubscribe<ConfigurationChangedMessage>(OnConfigurationChanged);

                SerialStatus?.Dispose();
                ApiStatus?.Dispose();
                QueueStatus?.Dispose();
                Settings?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}