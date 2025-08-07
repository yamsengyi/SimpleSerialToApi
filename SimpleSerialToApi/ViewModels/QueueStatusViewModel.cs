using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.ViewModels
{
    public class QueueStatusViewModel : ViewModelBase
    {
        private readonly ILogger<QueueStatusViewModel> _logger;
        private readonly IMessenger _messenger;

        private int _currentQueueSize = 0;
        private int _maxQueueSize = 1000;
        private int _totalProcessed = 0;
        private int _totalErrors = 0;
        private double _processingRate = 0;
        private string _queueHealth = "Good";
        private DateTime _lastProcessed = DateTime.MinValue;
        private bool _isProcessing = false;
        private int _retryCount = 0;

        public QueueStatusViewModel(
            ILogger<QueueStatusViewModel> logger,
            IMessenger messenger)
        {
            _logger = logger;
            _messenger = messenger;

            InitializeCommands();
            StartMonitoring();
        }

        public int CurrentQueueSize
        {
            get => _currentQueueSize;
            set
            {
                SetProperty(ref _currentQueueSize, value);
                UpdateQueueHealth();
                OnPropertyChanged(nameof(QueueUtilizationPercentage));
            }
        }

        public int MaxQueueSize
        {
            get => _maxQueueSize;
            set
            {
                SetProperty(ref _maxQueueSize, value);
                UpdateQueueHealth();
                OnPropertyChanged(nameof(QueueUtilizationPercentage));
            }
        }

        public double QueueUtilizationPercentage =>
            MaxQueueSize > 0 ? (double)CurrentQueueSize / MaxQueueSize * 100 : 0;

        public int TotalProcessed
        {
            get => _totalProcessed;
            set => SetProperty(ref _totalProcessed, value);
        }

        public int TotalErrors
        {
            get => _totalErrors;
            set => SetProperty(ref _totalErrors, value);
        }

        public double ProcessingRate
        {
            get => _processingRate;
            set => SetProperty(ref _processingRate, value);
        }

        public string QueueHealth
        {
            get => _queueHealth;
            set => SetProperty(ref _queueHealth, value);
        }

        public DateTime LastProcessed
        {
            get => _lastProcessed;
            set => SetProperty(ref _lastProcessed, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public int RetryCount
        {
            get => _retryCount;
            set => SetProperty(ref _retryCount, value);
        }

        public double SuccessRate =>
            TotalProcessed + TotalErrors > 0 ? (double)TotalProcessed / (TotalProcessed + TotalErrors) * 100 : 0;

        public ICommand StartProcessingCommand { get; private set; } = null!;
        public ICommand StopProcessingCommand { get; private set; } = null!;
        public ICommand ClearQueueCommand { get; private set; } = null!;
        public ICommand ResetStatisticsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            StartProcessingCommand = new RelayCommand(ExecuteStartProcessing, CanStartProcessing);
            StopProcessingCommand = new RelayCommand(ExecuteStopProcessing, CanStopProcessing);
            ClearQueueCommand = new RelayCommand(ExecuteClearQueue, CanClearQueue);
            ResetStatisticsCommand = new RelayCommand(ExecuteResetStatistics);
        }

        private void StartMonitoring()
        {
            // Start a timer to simulate queue activity
            var timer = new System.Timers.Timer(2000); // Update every 2 seconds
            timer.Elapsed += OnMonitoringTick;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnMonitoringTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!IsProcessing) return;

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                SimulateQueueActivity();
            });
        }

        private void SimulateQueueActivity()
        {
            var random = new Random();
            
            // Simulate messages being added to queue
            var newMessages = random.Next(0, 5);
            CurrentQueueSize = Math.Max(0, CurrentQueueSize + newMessages);
            
            // Simulate messages being processed
            if (CurrentQueueSize > 0)
            {
                var processed = Math.Min(CurrentQueueSize, random.Next(1, 4));
                CurrentQueueSize -= processed;
                TotalProcessed += processed;
                LastProcessed = DateTime.Now;
                
                // Simulate occasional errors
                if (random.Next(1, 101) <= 5) // 5% error rate
                {
                    TotalErrors++;
                    RetryCount++;
                }
                
                // Calculate processing rate (messages per minute)
                ProcessingRate = TotalProcessed / Math.Max(1, (DateTime.Now - GetStartTime()).TotalMinutes);
            }
            
            OnPropertyChanged(nameof(SuccessRate));
        }

        private DateTime GetStartTime()
        {
            // This would be stored when processing actually starts
            return DateTime.Now.AddMinutes(-5); // Simulate 5 minutes of processing
        }

        private void ExecuteStartProcessing()
        {
            IsProcessing = true;
            _logger.LogInformation("Queue processing started");
            
            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "Queue",
                Message = "Queue processing started"
            });
        }

        private void ExecuteStopProcessing()
        {
            IsProcessing = false;
            _logger.LogInformation("Queue processing stopped");
            
            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "Queue",
                Message = "Queue processing stopped"
            });
        }

        private void ExecuteClearQueue()
        {
            var clearedCount = CurrentQueueSize;
            CurrentQueueSize = 0;
            
            _logger.LogInformation("Queue cleared - {Count} messages removed", clearedCount);
            
            _messenger.Send(new LogMessage
            {
                Level = "WARNING",
                Source = "Queue",
                Message = $"Queue cleared - {clearedCount} messages removed"
            });
        }

        private void ExecuteResetStatistics()
        {
            TotalProcessed = 0;
            TotalErrors = 0;
            ProcessingRate = 0;
            RetryCount = 0;
            LastProcessed = DateTime.MinValue;
            
            _logger.LogInformation("Queue statistics reset");
            
            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "Queue",
                Message = "Queue statistics reset"
            });
        }

        private bool CanStartProcessing() => !IsProcessing;
        private bool CanStopProcessing() => IsProcessing;
        private bool CanClearQueue() => CurrentQueueSize > 0;

        private void UpdateQueueHealth()
        {
            var utilizationPercentage = QueueUtilizationPercentage;
            
            QueueHealth = utilizationPercentage switch
            {
                < 50 => "Good",
                < 80 => "Fair",
                < 95 => "Poor",
                _ => "Critical"
            };

            // Send alert if queue is getting full
            if (utilizationPercentage > 90)
            {
                _messenger.Send(new LogMessage
                {
                    Level = "WARNING",
                    Source = "Queue",
                    Message = $"Queue utilization high: {utilizationPercentage:F1}% ({CurrentQueueSize}/{MaxQueueSize})"
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up timer if stored as field
            }
            base.Dispose(disposing);
        }
    }
}