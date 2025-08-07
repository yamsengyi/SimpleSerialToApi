using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;

namespace SimpleSerialToApi.ViewModels
{
    public class ApiStatusViewModel : ViewModelBase
    {
        private readonly ILogger<ApiStatusViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly IHttpApiClientService _apiService;

        private ConnectionStatus _overallStatus = ConnectionStatus.Disconnected;
        private string _statusMessage = "No API connections";
        private int _totalCalls = 0;
        private int _successfulCalls = 0;
        private int _failedCalls = 0;
        private double _successRate = 0;
        private double _averageResponseTime = 0;

        public ApiStatusViewModel(
            ILogger<ApiStatusViewModel> logger,
            IMessenger messenger,
            IHttpApiClientService apiService)
        {
            _logger = logger;
            _messenger = messenger;
            _apiService = apiService;

            ApiEndpoints = new ObservableCollection<ApiEndpointStatus>();
            InitializeCommands();
            LoadApiEndpoints();
        }

        public ConnectionStatus OverallStatus
        {
            get => _overallStatus;
            set => SetProperty(ref _overallStatus, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int TotalCalls
        {
            get => _totalCalls;
            set => SetProperty(ref _totalCalls, value);
        }

        public int SuccessfulCalls
        {
            get => _successfulCalls;
            set => SetProperty(ref _successfulCalls, value);
        }

        public int FailedCalls
        {
            get => _failedCalls;
            set => SetProperty(ref _failedCalls, value);
        }

        public double SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }

        public double AverageResponseTime
        {
            get => _averageResponseTime;
            set => SetProperty(ref _averageResponseTime, value);
        }

        public ObservableCollection<ApiEndpointStatus> ApiEndpoints { get; }

        public ICommand TestAllConnectionsCommand { get; private set; } = null!;
        public ICommand TestConnectionCommand { get; private set; } = null!;
        public ICommand RefreshStatusCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            TestAllConnectionsCommand = new RelayCommand(async () => await ExecuteTestAllConnectionsAsync(), CanTestConnections);
            TestConnectionCommand = new RelayCommand<ApiEndpointStatus>(async (endpoint) => await ExecuteTestConnectionAsync(endpoint), CanTestConnection);
            RefreshStatusCommand = new RelayCommand(ExecuteRefreshStatus);
        }

        private void LoadApiEndpoints()
        {
            try
            {
                // This would typically load from configuration
                // For now, we'll create sample endpoints
                ApiEndpoints.Clear();
                
                var sampleEndpoints = new[]
                {
                    new ApiEndpointStatus
                    {
                        Name = "Primary API",
                        Url = "https://api.example.com/data",
                        Status = ConnectionStatus.Disconnected
                    },
                    new ApiEndpointStatus
                    {
                        Name = "Backup API",
                        Url = "https://backup.api.example.com/data",
                        Status = ConnectionStatus.Disconnected
                    }
                };

                foreach (var endpoint in sampleEndpoints)
                {
                    ApiEndpoints.Add(endpoint);
                }

                UpdateOverallStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load API endpoints");
            }
        }

        private async Task ExecuteTestAllConnectionsAsync()
        {
            _logger.LogInformation("Testing all API connections...");
            
            foreach (var endpoint in ApiEndpoints)
            {
                await TestEndpointConnection(endpoint);
            }

            UpdateOverallStatus();
            UpdateStatistics();

            _messenger.Send(new LogMessage
            {
                Level = "INFO",
                Source = "API",
                Message = $"Tested {ApiEndpoints.Count} API endpoints"
            });
        }

        private async Task ExecuteTestConnectionAsync(ApiEndpointStatus? endpoint)
        {
            if (endpoint == null) return;

            _logger.LogInformation("Testing API connection: {EndpointName}", endpoint.Name);
            await TestEndpointConnection(endpoint);
            UpdateOverallStatus();
        }

        private async Task TestEndpointConnection(ApiEndpointStatus endpoint)
        {
            try
            {
                endpoint.Status = ConnectionStatus.Connecting;
                var startTime = DateTime.Now;

                // Simulate API call - in real implementation, use actual API service
                await Task.Delay(500); // Simulate network call
                
                // For demonstration, randomly succeed or fail
                var random = new Random();
                var success = random.Next(1, 101) <= 80; // 80% success rate

                var responseTime = (DateTime.Now - startTime).TotalMilliseconds;

                if (success)
                {
                    endpoint.Status = ConnectionStatus.Connected;
                    endpoint.LastSuccessfulCall = DateTime.Now;
                    endpoint.SuccessfulCalls++;
                    endpoint.LastError = null;
                }
                else
                {
                    endpoint.Status = ConnectionStatus.Error;
                    endpoint.LastError = "Connection timeout";
                }

                endpoint.TotalCalls++;
                endpoint.AverageResponseTime = responseTime;

                _logger.LogInformation("API test completed: {EndpointName} - {Status}", 
                    endpoint.Name, endpoint.Status);
            }
            catch (Exception ex)
            {
                endpoint.Status = ConnectionStatus.Error;
                endpoint.LastError = ex.Message;
                _logger.LogError(ex, "API connection test failed for {EndpointName}", endpoint.Name);
            }
        }

        private void ExecuteRefreshStatus()
        {
            UpdateOverallStatus();
            UpdateStatistics();
            _logger.LogInformation("API status refreshed");
        }

        private void UpdateOverallStatus()
        {
            if (!ApiEndpoints.Any())
            {
                OverallStatus = ConnectionStatus.Disconnected;
                StatusMessage = "No API endpoints configured";
                return;
            }

            var connectedCount = ApiEndpoints.Count(e => e.Status == ConnectionStatus.Connected);
            var totalCount = ApiEndpoints.Count;

            OverallStatus = connectedCount switch
            {
                0 => ConnectionStatus.Disconnected,
                _ when connectedCount == totalCount => ConnectionStatus.Connected,
                _ => ConnectionStatus.Error // Partial connectivity
            };

            StatusMessage = connectedCount switch
            {
                0 => "All API endpoints disconnected",
                _ when connectedCount == totalCount => "All API endpoints connected",
                _ => $"{connectedCount}/{totalCount} API endpoints connected"
            };

            _messenger.Send(new StatusUpdatedMessage
            {
                ComponentName = "API",
                Status = OverallStatus.ToString()
            });
        }

        private void UpdateStatistics()
        {
            TotalCalls = ApiEndpoints.Sum(e => e.TotalCalls);
            SuccessfulCalls = ApiEndpoints.Sum(e => e.SuccessfulCalls);
            FailedCalls = TotalCalls - SuccessfulCalls;
            SuccessRate = TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls * 100 : 0;
            AverageResponseTime = ApiEndpoints.Any() ? ApiEndpoints.Average(e => e.AverageResponseTime) : 0;
        }

        private bool CanTestConnections() => ApiEndpoints.Any();
        private bool CanTestConnection(ApiEndpointStatus? endpoint) => endpoint != null;

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