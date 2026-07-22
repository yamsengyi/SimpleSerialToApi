using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Facades;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.ViewModels
{
    /// <summary>
    /// 메인 ViewModel — 7개 파사드로 책임 분산 (리팩토링 v2)
    /// 직접 의존성: 13개 → 8개 (파사드 7개 + SerialDataSimulator)
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly ISimulationFacade _simulationFacade;
        private readonly IConfigurationFacade _configurationFacade;
        private readonly IMonitorFacade _monitorFacade;
        private readonly ISerialConnectionFacade _serialConnectionFacade;
        private readonly IDataMappingFacade _dataMappingFacade;
        private readonly IWindowManagementFacade _windowManagementFacade;
        private readonly IDataTransmissionFacade _dataTransmissionFacade;
        private readonly SerialDataSimulator _serialDataSimulator;

        private string _status = "Disconnected";
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public MainViewModel(
            ILogger<MainViewModel> logger,
            ISimulationFacade simulationFacade,
            IConfigurationFacade configurationFacade,
            IMonitorFacade monitorFacade,
            ISerialConnectionFacade serialConnectionFacade,
            IDataMappingFacade dataMappingFacade,
            IWindowManagementFacade windowManagementFacade,
            IDataTransmissionFacade dataTransmissionFacade,
            SerialDataSimulator serialDataSimulator)
        {
            _logger = logger;
            _simulationFacade = simulationFacade;
            _configurationFacade = configurationFacade;
            _monitorFacade = monitorFacade;
            _serialConnectionFacade = serialConnectionFacade;
            _dataMappingFacade = dataMappingFacade;
            _windowManagementFacade = windowManagementFacade;
            _dataTransmissionFacade = dataTransmissionFacade;
            _serialDataSimulator = serialDataSimulator;

            // 시뮬레이터 이벤트 구독
            _serialDataSimulator.DataGenerated += OnSimulatedDataReceived;

            // 데이터 전송 이벤트 구독
            _dataTransmissionFacade.QueueCountChanged += (_, count) =>
            {
                OnPropertyChanged(nameof(QueueCount));
            };
            _dataTransmissionFacade.StatusChanged += (_, status) =>
            {
                Status = status;
            };

            // 파사드 속성 변경 구독 (UI 바인딩 전파)
            SubscribeFacadePropertyChanges();

            // Commands 초기화
            InitializeCommands();

            // 서비스 이벤트 구독
            SubscribeServiceEvents();

            // 초기화 작업
            InitializeAsync();
        }

        // ───────────────────── Properties (파사드에 위임) ─────────────────────

        public string SerialPort
        {
            get => _serialConnectionFacade.SerialPort;
            set => _serialConnectionFacade.SerialPort = value;
        }

        public string ApiUrl
        {
            get => _configurationFacade.ApiUrl;
            set => _configurationFacade.ApiUrl = value;
        }

        public bool IsConnected
        {
            get => _serialConnectionFacade.IsConnected;
            set => _serialConnectionFacade.IsConnected = value;
        }

        public int QueueCount => _dataTransmissionFacade.QueueCount;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ComPortInfo> AvailablePorts =>
            _serialConnectionFacade.AvailablePorts;

        public string TransmissionInterval
        {
            get => _configurationFacade.TransmissionInterval;
            set => _configurationFacade.TransmissionInterval = value;
        }

        public string BatchSize
        {
            get => _configurationFacade.BatchSize;
            set => _configurationFacade.BatchSize = value;
        }

        public string DeviceId
        {
            get => _configurationFacade.DeviceId;
            set => _configurationFacade.DeviceId = value;
        }

        // ─── Monitor Properties ───

        public string SerialMonitorText
        {
            get => _monitorFacade.SerialMonitorText;
            set => _monitorFacade.SerialMonitorText = value;
        }

        public string ApiMonitorText
        {
            get => _monitorFacade.ApiMonitorText;
            set => _monitorFacade.ApiMonitorText = value;
        }

        public bool SerialMonitorAutoScroll
        {
            get => _monitorFacade.SerialMonitorAutoScroll;
            set => _monitorFacade.SerialMonitorAutoScroll = value;
        }

        public bool ApiMonitorAutoScroll
        {
            get => _monitorFacade.ApiMonitorAutoScroll;
            set => _monitorFacade.ApiMonitorAutoScroll = value;
        }

        public string SerialMonitorStatus
        {
            get => _monitorFacade.SerialMonitorStatus;
            set => _monitorFacade.SerialMonitorStatus = value;
        }

        public string ApiMonitorStatus
        {
            get => _monitorFacade.ApiMonitorStatus;
            set => _monitorFacade.ApiMonitorStatus = value;
        }

        public string SerialMonitorFilter
        {
            get => _monitorFacade.SerialMonitorFilter;
            set => _monitorFacade.SerialMonitorFilter = value;
        }

        public string ApiMonitorFilter
        {
            get => _monitorFacade.ApiMonitorFilter;
            set => _monitorFacade.ApiMonitorFilter = value;
        }

        public System.Collections.Generic.List<string> SerialMonitorFilters =>
            _monitorFacade.SerialMonitorFilters;

        public System.Collections.Generic.List<string> ApiMonitorFilters =>
            _monitorFacade.ApiMonitorFilters;

        public bool SerialShowTimestamps
        {
            get => _monitorFacade.SerialShowTimestamps;
            set => _monitorFacade.SerialShowTimestamps = value;
        }

        public bool ApiShowHeaders
        {
            get => _monitorFacade.ApiShowHeaders;
            set => _monitorFacade.ApiShowHeaders = value;
        }

        public string SerialMessageCount => _monitorFacade.SerialMessageCount;
        public string ApiRequestCount => _monitorFacade.ApiRequestCount;
        public string ApiSuccessRate => _monitorFacade.ApiSuccessRate;

        // ─── Simulation Properties ───

        public bool IsSimulating => _simulationFacade.IsSimulating;

        public string SimulationInterval
        {
            get => _simulationFacade.SimulationInterval;
            set => _simulationFacade.SimulationInterval = value;
        }

        public string SimulationButtonText => _simulationFacade.SimulationButtonText;

        // ─── Data Mapping Properties ───

        public ObservableCollection<DataMappingScenario> MappingScenarios =>
            _dataMappingFacade.MappingScenarios;

        public DataMappingScenario? SelectedMappingScenario
        {
            get => _dataMappingFacade.SelectedMappingScenario;
            set => _dataMappingFacade.SelectedMappingScenario = value;
        }

        public System.Collections.Generic.List<DataSource> DataSources =>
            _dataMappingFacade.DataSources;

        public System.Collections.Generic.List<TransmissionType> TransmissionTypes =>
            _dataMappingFacade.TransmissionTypes;

        public System.Collections.Generic.List<string> ApiMethods =>
            _dataMappingFacade.ApiMethods;

        public System.Collections.Generic.List<string> ContentTypes =>
            _dataMappingFacade.ContentTypes;

        public string MappingScenariosCount => _dataMappingFacade.MappingScenariosCount;
        public string SerialConnectionStatus => _serialConnectionFacade.SerialConnectionStatus;
        public string ApiEndpointStatus => _configurationFacade.ApiUrl;

        public event EventHandler<bool>? DataMappingWindowCloseRequested;

        // ───────────────────── Commands ─────────────────────

        public ICommand ConnectCommand { get; private set; } = null!;
        public ICommand DisconnectCommand { get; private set; } = null!;
        public ICommand TestApiCommand { get; private set; } = null!;
        public ICommand RefreshPortsCommand { get; private set; } = null!;
        public ICommand OpenSerialConfigCommand { get; private set; } = null!;
        public ICommand SetTransmissionIntervalCommand { get; private set; } = null!;
        public ICommand SetBatchSizeCommand { get; private set; } = null!;
        public ICommand SetDeviceIdCommand { get; private set; } = null!;
        public ICommand AddMappingScenarioCommand { get; private set; } = null!;
        public ICommand DeleteMappingScenarioCommand { get; private set; } = null!;
        public ICommand MoveUpMappingScenarioCommand { get; private set; } = null!;
        public ICommand MoveDownMappingScenarioCommand { get; private set; } = null!;
        public ICommand CopyMappingScenarioCommand { get; private set; } = null!;
        public ICommand TestMappingCommand { get; private set; } = null!;
        public ICommand SaveMappingCommand { get; private set; } = null!;
        public ICommand ShowReservedWordsCommand { get; private set; } = null!;
        public ICommand ApplyCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand SaveSerialMonitorCommand { get; private set; } = null!;
        public ICommand SaveApiMonitorCommand { get; private set; } = null!;
        public ICommand ClearSerialMonitorCommand { get; private set; } = null!;
        public ICommand ClearApiMonitorCommand { get; private set; } = null!;
        public ICommand OpenDataMappingCommand { get; private set; } = null!;
        public ICommand OpenSerialMonitorCommand { get; private set; } = null!;
        public ICommand OpenApiMonitorCommand { get; private set; } = null!;
        public ICommand StartSimulationCommand { get; private set; } = null!;
        public ICommand GenerateSingleDataCommand { get; private set; } = null!;
        public ICommand ClearQueueCommand { get; private set; } = null!;
        public ICommand ClearLogsCommand { get; private set; } = null!;

        // ───────────────────── Initialization ─────────────────────

        private void InitializeCommands()
        {
            ConnectCommand = new RelayCommand(async () =>
            {
                Status = "Connecting...";
                await _serialConnectionFacade.ConnectAsync();
                Status = _serialConnectionFacade.IsConnected
                    ? "Connected - API queue processing is active"
                    : "Connection Failed";
                _dataTransmissionFacade.UpdateQueueCount();
            }, () => _serialConnectionFacade.CanConnect);

            DisconnectCommand = new RelayCommand(async () =>
            {
                await _serialConnectionFacade.DisconnectAsync();
                Status = "Disconnected";
                _dataTransmissionFacade.UpdateQueueCount();
            }, () => _serialConnectionFacade.CanDisconnect);

            TestApiCommand = new RelayCommand(async () =>
            {
                await _dataTransmissionFacade.TestApiAsync(_configurationFacade.ApiUrl);
            });

            RefreshPortsCommand = new RelayCommand(() =>
            {
                _serialConnectionFacade.RefreshPorts();
            });

            OpenSerialConfigCommand = new RelayCommand(() =>
            {
                _configurationFacade.OpenSerialConfig();
            });

            SetTransmissionIntervalCommand = new RelayCommand(() =>
            {
                _configurationFacade.SetTransmissionInterval();
                Status = $"Transmission interval set to {TransmissionInterval} seconds";
            });

            SetBatchSizeCommand = new RelayCommand(() =>
            {
                _configurationFacade.SetBatchSize();
                Status = $"Batch size set to {BatchSize}";
            });

            SetDeviceIdCommand = new RelayCommand(() =>
            {
                _configurationFacade.SetDeviceId();
                Status = $"Device ID set to '{DeviceId}'";
            });

            AddMappingScenarioCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.AddScenario();
            });

            DeleteMappingScenarioCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.DeleteScenario();
            });

            MoveUpMappingScenarioCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.MoveUpScenario();
            }, () => SelectedMappingScenario != null);

            MoveDownMappingScenarioCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.MoveDownScenario();
            }, () => SelectedMappingScenario != null);

            CopyMappingScenarioCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.CopyScenario();
            }, () => SelectedMappingScenario != null);

            TestMappingCommand = new RelayCommand(async () =>
            {
                // 테스트 데이터 입력 대화상자
                var testInput = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter test serial data:",
                    "Mapping Test",
                    "[QR|01012345678|20240723143015|1]",
                    -1, -1);

                if (string.IsNullOrEmpty(testInput))
                    return;

                var displayItems = await _dataMappingFacade.TestMappingAsync(testInput);
                var testWindow = new Views.TestResultWindow(testInput, displayItems);
                testWindow.Owner = System.Windows.Application.Current.MainWindow;
                testWindow.ShowDialog();
            });

            SaveMappingCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.SaveMapping();
                Status = $"Saved {MappingScenarios.Count} mapping scenarios to file";
            });

            ShowReservedWordsCommand = new RelayCommand(() =>
            {
                _windowManagementFacade.ShowReservedWords();
            });

            ApplyCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.Apply();
            });

            CancelCommand = new RelayCommand(() =>
            {
                _dataMappingFacade.Cancel();
            });

            SaveSerialMonitorCommand = new RelayCommand(() =>
            {
                _monitorFacade.SaveSerialMonitor();
                Status = "Serial monitor saved";
            });

            SaveApiMonitorCommand = new RelayCommand(() =>
            {
                _monitorFacade.SaveApiMonitor();
                Status = "API monitor saved";
            });

            ClearSerialMonitorCommand = new RelayCommand(() =>
            {
                _monitorFacade.ClearSerialMonitor();
                Status = "Serial monitor cleared";
            });

            ClearApiMonitorCommand = new RelayCommand(() =>
            {
                _monitorFacade.ClearApiMonitor();
                Status = "API monitor cleared";
            });

            OpenDataMappingCommand = new RelayCommand(() =>
            {
                _windowManagementFacade.OpenDataMapping(this);
            });

            OpenSerialMonitorCommand = new RelayCommand(() =>
            {
                _windowManagementFacade.OpenSerialMonitor(this);
            });

            OpenApiMonitorCommand = new RelayCommand(() =>
            {
                _windowManagementFacade.OpenApiMonitor(this);
            });

            StartSimulationCommand = new RelayCommand(() =>
            {
                _simulationFacade.Toggle();
                Status = _simulationFacade.IsSimulating
                    ? "Simulation started"
                    : "Simulation stopped";
            });

            GenerateSingleDataCommand = new RelayCommand(() =>
            {
                _simulationFacade.GenerateSingleData();
                Status = "Single simulation data generated";
            });

            ClearQueueCommand = new RelayCommand(async () =>
            {
                await _dataTransmissionFacade.ClearQueueAsync();
                System.Windows.MessageBox.Show(
                    "메시지 큐가 성공적으로 삭제되었습니다.",
                    "Queue 클리어",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            });

            ClearLogsCommand = new RelayCommand(() =>
            {
                _dataTransmissionFacade.ClearLogs();
                System.Windows.MessageBox.Show(
                    "모든 모니터 로그가 성공적으로 삭제되었습니다.",
                    "로그 클리어",
                    System.Windows.MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        private void SubscribeServiceEvents()
        {
            // SerialCommunicationService events
            var serialService = GetSerialService();
            serialService.DataReceived += async (_, e) =>
            {
                await _dataTransmissionFacade.OnSerialDataReceivedAsync(e.Data);
            };
            serialService.ConnectionStatusChanged += (_, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    IsConnected = e.IsConnected;
                    Status = e.Message;
                });
            };

            // DataMappingService.MappingProcessed → DataTransmissionFacade
            var dataMappingService = GetDataMappingService();
            dataMappingService.MappingProcessed += async (_, e) =>
            {
                await _dataTransmissionFacade.OnMappingProcessedAsync(e);
            };

            // Monitor service events → MonitorFacade
            var serialMonitorService = GetSerialMonitorService();
            serialMonitorService.MessageAdded += (_, msg) =>
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                    _monitorFacade.OnSerialMonitorMessageAdded(msg));
            };

            var apiMonitorService = GetApiMonitorService();
            apiMonitorService.MessageAdded += (_, msg) =>
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                    _monitorFacade.OnApiMonitorMessageAdded(msg));
            };
        }

        private void SubscribeFacadePropertyChanges()
        {
            if (_simulationFacade is INotifyPropertyChanged simNpc)
                simNpc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ISimulationFacade.IsSimulating))
                        OnPropertyChanged(nameof(IsSimulating));
                    else if (e.PropertyName == nameof(ISimulationFacade.SimulationButtonText))
                        OnPropertyChanged(nameof(SimulationButtonText));
                    else if (e.PropertyName == nameof(ISimulationFacade.SimulationInterval))
                        OnPropertyChanged(nameof(SimulationInterval));
                };

            if (_serialConnectionFacade is INotifyPropertyChanged serNpc)
                serNpc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ISerialConnectionFacade.IsConnected))
                        OnPropertyChanged(nameof(IsConnected));
                    else if (e.PropertyName == nameof(ISerialConnectionFacade.SerialPort))
                        OnPropertyChanged(nameof(SerialPort));
                    else if (e.PropertyName == nameof(ISerialConnectionFacade.SerialConnectionStatus))
                        OnPropertyChanged(nameof(SerialConnectionStatus));
                };

            if (_monitorFacade is INotifyPropertyChanged monNpc)
                monNpc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(IMonitorFacade.SerialMessageCount))
                        OnPropertyChanged(nameof(SerialMessageCount));
                    else if (e.PropertyName == nameof(IMonitorFacade.ApiRequestCount))
                        OnPropertyChanged(nameof(ApiRequestCount));
                    else if (e.PropertyName == nameof(IMonitorFacade.ApiSuccessRate))
                        OnPropertyChanged(nameof(ApiSuccessRate));
                };

            if (_dataMappingFacade is INotifyPropertyChanged mapNpc)
                mapNpc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(IDataMappingFacade.MappingScenariosCount))
                        OnPropertyChanged(nameof(MappingScenariosCount));
                };

            _dataMappingFacade.DataMappingWindowCloseRequested += (_, result) =>
            {
                DataMappingWindowCloseRequested?.Invoke(this, result);
            };
        }

        private async void InitializeAsync()
        {
            _configurationFacade.LoadApiUrl();
            _configurationFacade.LoadQueueSettings();
            _dataMappingFacade.InitializeScenarios();
            _serialConnectionFacade.RefreshPorts();
            _serialConnectionFacade.InitializeSmartPortSelection();
            _dataTransmissionFacade.UpdateQueueCount();

            _ = Task.Run(
                async () => await _dataTransmissionFacade.InitializeQueueProcessingAsync(),
                _cancellationTokenSource.Token);

            _ = Task.Run(
                () => _serialConnectionFacade.CheckAutoConnectAsync(),
                _cancellationTokenSource.Token);

            await Task.CompletedTask;
        }

        // ───────────────────── Helper: 서비스 접근자 ─────────────────────
        // (cast to concrete facade types to access internal services)

        private SerialCommunicationService GetSerialService() =>
            ((SerialConnectionFacade)_serialConnectionFacade).GetService();

        private DataMappingService GetDataMappingService() =>
            ((DataMappingFacade)_dataMappingFacade).GetService();

        private SerialMonitorService GetSerialMonitorService() =>
            ((MonitorFacade)_monitorFacade).GetSerialMonitorService();

        private ApiMonitorService GetApiMonitorService() =>
            ((MonitorFacade)_monitorFacade).GetApiMonitorService();

        // ───────────────────── Event Handlers ─────────────────────

        private async void OnSimulatedDataReceived(object? sender, SimulatedSerialDataEventArgs e)
        {
            await _dataTransmissionFacade.OnSimulatedDataReceivedAsync(e);
        }

        // ───────────────────── INotifyPropertyChanged ─────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ───────────────────── Child Window Management ─────────────────────

        public void CloseAllChildWindows()
        {
            _windowManagementFacade.CloseAllChildWindows();
        }

        // ───────────────────── IDisposable ─────────────────────

        public void Dispose()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();

                if (_simulationFacade.IsSimulating)
                    _simulationFacade.Stop();

                _serialDataSimulator.DataGenerated -= OnSimulatedDataReceived;

                CloseAllChildWindows();
                _cancellationTokenSource.Dispose();

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during MainViewModel disposal");
            }
        }
    }
}
