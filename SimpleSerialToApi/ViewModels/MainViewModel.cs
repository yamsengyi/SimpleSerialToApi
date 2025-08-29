using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Interfaces;

namespace SimpleSerialToApi.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly SerialCommunicationService _serialService;
        private readonly SimpleQueueService _queueService;
        private readonly SimpleHttpService _httpService;
        private readonly TimerService _timerService;
        private readonly ComPortDiscoveryService _comPortDiscovery;
        private readonly DataMappingService _dataMappingService;
        private readonly SerialMonitorService _serialMonitorService;
        private readonly ApiMonitorService _apiMonitorService;
        private readonly ReservedWordService _reservedWordService;
        private readonly SerialDataSimulator _serialDataSimulator;
        private readonly IConfigurationService _configurationService;
        private readonly IQueueManager _queueManager;
        private readonly IQueueProcessor<MappedApiData> _apiDataQueueProcessor;

        private string _serialPort = "COM1";
        private string _apiUrl = "http://localhost:8080/api/data"; // 연결 테스트 전용 URL
        private bool _isConnected = false;
        private bool _isTimerRunning = false;
        private int _queueCount = 0;
        private string _status = "Disconnected";
        private ObservableCollection<ComPortInfo> _availablePorts = new();
        
        // Queue 관리 관련 필드
        private string _transmissionInterval = "5";
        private string _batchSize = "10";
        
        // Device ID 필드
        private string _deviceId = string.Empty;
        
        // Monitor 관련 필드
        private bool _serialMonitorVisible = false;
        private bool _apiMonitorVisible = false;
        
        // 시뮬레이션 관련 필드
        private bool _isSimulating = false;
        private string _simulationInterval = "3";
        
        // 창 인스턴스 추적 필드
        private Views.DataMappingWindow? _dataMappingWindow;
        private Views.ReservedWordsWindow? _reservedWordsWindow;
        private Views.SerialMonitorWindow? _serialMonitorWindow;
        private Views.ApiMonitorWindow? _apiMonitorWindow;
        
        // 백그라운드 작업 관리
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public MainViewModel(
            ILogger<MainViewModel> logger,
            SerialCommunicationService serialService,
            SimpleQueueService queueService,
            SimpleHttpService httpService,
            TimerService timerService,
            ComPortDiscoveryService comPortDiscovery,
            DataMappingService dataMappingService,
            SerialMonitorService serialMonitorService,
            ApiMonitorService apiMonitorService,
            ReservedWordService reservedWordService,
            SerialDataSimulator serialDataSimulator,
            IConfigurationService configurationService,
            IQueueManager queueManager,
            IQueueProcessor<MappedApiData> apiDataQueueProcessor)
        {
            _logger = logger;
            _serialService = serialService;
            _queueService = queueService;
            _httpService = httpService;
            _timerService = timerService;
            _comPortDiscovery = comPortDiscovery;
            _dataMappingService = dataMappingService;
            _serialMonitorService = serialMonitorService;
            _apiMonitorService = apiMonitorService;
            _reservedWordService = reservedWordService;
            _configurationService = configurationService;
            _queueManager = queueManager;
            _apiDataQueueProcessor = apiDataQueueProcessor;

            // 시뮬레이터 초기화
            _serialDataSimulator = serialDataSimulator;
            _serialDataSimulator.DataGenerated += OnSimulatedDataReceived;

            // Commands
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);
            TestApiCommand = new RelayCommand(TestApi);
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            OpenSerialConfigCommand = new RelayCommand(OpenSerialConfig);
            SetTransmissionIntervalCommand = new RelayCommand(SetTransmissionInterval);
            SetBatchSizeCommand = new RelayCommand(SetBatchSize);
            SetDeviceIdCommand = new RelayCommand(SetDeviceId);
            AddMappingScenarioCommand = new RelayCommand(AddMappingScenario);
            DeleteMappingScenarioCommand = new RelayCommand(DeleteMappingScenario);
            TestMappingCommand = new RelayCommand(TestMapping);
            SaveMappingCommand = new RelayCommand(SaveMapping);
            ShowReservedWordsCommand = new RelayCommand(ShowReservedWords);
            ApplyCommand = new RelayCommand(ApplyDataMapping);
            CancelCommand = new RelayCommand(CancelDataMapping);
            ToggleSerialMonitorCommand = new RelayCommand(ToggleSerialMonitor);
            ToggleApiMonitorCommand = new RelayCommand(ToggleApiMonitor);
            SaveSerialMonitorCommand = new RelayCommand(SaveSerialMonitor);
            SaveApiMonitorCommand = new RelayCommand(SaveApiMonitor);
            ShowSerialMonitorCommand = new RelayCommand(ShowSerialMonitor);
            HideSerialMonitorCommand = new RelayCommand(HideSerialMonitor);
            ShowApiMonitorCommand = new RelayCommand(ShowApiMonitor);
            HideApiMonitorCommand = new RelayCommand(HideApiMonitor);
            ClearSerialMonitorCommand = new RelayCommand(ClearSerialMonitor);
            ClearApiMonitorCommand = new RelayCommand(ClearApiMonitor);
            
            // 새로운 팝업 창 명령들
            OpenDataMappingCommand = new RelayCommand(OpenDataMapping);
            OpenSerialMonitorCommand = new RelayCommand(OpenSerialMonitor);
            OpenApiMonitorCommand = new RelayCommand(OpenApiMonitor);
            
            // 시뮬레이션 명령들
            StartSimulationCommand = new RelayCommand(ToggleSimulation);
            StopSimulationCommand = new RelayCommand(StopSimulation);
            GenerateSingleDataCommand = new RelayCommand(GenerateSingleData);
            
            // Queue 및 로그 클리어 명령들
            ClearQueueCommand = new RelayCommand(ClearQueue);
            ClearLogsCommand = new RelayCommand(ClearLogs);

            // 이벤트 구독
            _serialService.DataReceived += OnSerialDataReceived;
            _serialService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _timerService.QueueProcessed += OnQueueProcessed;
            _dataMappingService.MappingProcessed += OnMappingProcessed;
            
            // 모니터 서비스 이벤트 구독
            _serialMonitorService.MessageAdded += OnSerialMonitorMessageAdded;
            _apiMonitorService.MessageAdded += OnApiMonitorMessageAdded;

            // ConfigurationService에서 API URL 로드
            LoadApiUrl();

            // API URL 설정
            _httpService.SetApiUrl(_apiUrl);

            // App.config에서 설정 읽기
            LoadQueueSettings();
            
            // 매핑 시나리오 초기화
            InitializeMappingScenarios();

            // 초기 포트 목록 로드 및 스마트 선택
            RefreshPorts();
            InitializeSmartPortSelection();
            
            // 초기 Queue 상태 업데이트
            UpdateQueueCount();
            
            // 큐 매니저 초기화 및 API 데이터 처리 시작 (조건부)
            _ = Task.Run(async () => await InitializeQueueProcessingConditional(), _cancellationTokenSource.Token);
            
            // 자동 연결 확인
            _ = Task.Run(CheckAutoConnect, _cancellationTokenSource.Token);
        }

        // Properties
        public string SerialPort
        {
            get => _serialPort;
            set 
            { 
                _serialPort = value; 
                OnPropertyChanged();
                
                // SerialCommunicationService에 포트 업데이트
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _serialService.UpdatePortName(value);
                }
            }
        }

        /// <summary>
        /// API URL for connection testing only (not used for actual data transmission)
        /// 연결 테스트 전용 API URL (실제 데이터 전송에는 매핑 테이블의 URL 사용)
        /// </summary>
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

        public ObservableCollection<ComPortInfo> AvailablePorts
        {
            get => _availablePorts;
            set { _availablePorts = value; OnPropertyChanged(); }
        }
        
        // Queue 관리 속성
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
        
        // Device ID 속성
        public string DeviceId
        {
            get => _deviceId;
            set { _deviceId = value; OnPropertyChanged(); }
        }
        
        // Monitor 속성
        public bool SerialMonitorVisible
        {
            get => _serialMonitorVisible;
            set { _serialMonitorVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(SerialMonitorButtonText)); }
        }
        
        // 추가 모니터 관련 속성들 (중복 제거)
        private string _serialMonitorText = string.Empty;
        public string SerialMonitorText
        {
            get => _serialMonitorText;
            set { _serialMonitorText = value; OnPropertyChanged(); }
        }
        
        private string _apiMonitorText = string.Empty;
        public string ApiMonitorText
        {
            get => _apiMonitorText;
            set { _apiMonitorText = value; OnPropertyChanged(); }
        }
        
        private bool _serialMonitorAutoScroll = true;
        public bool SerialMonitorAutoScroll
        {
            get => _serialMonitorAutoScroll;
            set { _serialMonitorAutoScroll = value; OnPropertyChanged(); }
        }
        
        private bool _apiMonitorAutoScroll = true;
        public bool ApiMonitorAutoScroll
        {
            get => _apiMonitorAutoScroll;
            set { _apiMonitorAutoScroll = value; OnPropertyChanged(); }
        }
        
        private string _serialMonitorStatus = "Ready";
        public string SerialMonitorStatus
        {
            get => _serialMonitorStatus;
            set { _serialMonitorStatus = value; OnPropertyChanged(); }
        }
        
        private string _apiMonitorStatus = "Ready";
        public string ApiMonitorStatus
        {
            get => _apiMonitorStatus;
            set { _apiMonitorStatus = value; OnPropertyChanged(); }
        }
        
        public string SerialMonitorButtonText => SerialMonitorVisible ? "Hide Serial Monitor" : "Show Serial Monitor";
        public string ApiMonitorButtonText => ApiMonitorVisible ? "Hide API Monitor" : "Show API Monitor";
        
        private string _apiMonitorFilter = "All";
        public string ApiMonitorFilter
        {
            get => _apiMonitorFilter;
            set { _apiMonitorFilter = value; OnPropertyChanged(); }
        }
        
        public List<string> ApiMonitorFilters { get; } = new() { "All", "2xx", "4xx", "5xx", "GET", "POST", "PUT", "DELETE" };
        
        public bool ApiMonitorVisible
        {
            get => _apiMonitorVisible;
            set { _apiMonitorVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(ApiMonitorButtonText)); }
        }
        
        // 시뮬레이션 관련 프로퍼티
        public bool IsSimulating
        {
            get => _isSimulating;
            set { _isSimulating = value; OnPropertyChanged(); OnPropertyChanged(nameof(SimulationButtonText)); }
        }
        
        public string SimulationInterval
        {
            get => _simulationInterval;
            set { _simulationInterval = value; OnPropertyChanged(); }
        }
        
        public string SimulationButtonText => IsSimulating ? "Stop Simulation" : "Start Simulation";
        
        // Data Mapping 시나리오 컬렉션
        public ObservableCollection<DataMappingScenario> MappingScenarios { get; } = new();
        
        // DataGrid 지원 속성들
        public List<DataSource> DataSources { get; } = new() { DataSource.Serial, DataSource.ApiResponse };
        public List<TransmissionType> TransmissionTypes { get; } = new() { TransmissionType.Serial, TransmissionType.Api };
        public List<string> ApiMethods { get; } = new() { "GET", "POST", "PUT", "DELETE" };
        public List<string> ContentTypes { get; } = new() 
        { 
            "application/json", 
            "application/xml", 
            "text/plain", 
            "text/html", 
            "text/xml", 
            "application/x-www-form-urlencoded",
            "multipart/form-data",
            "text/csv"
        };
        
        private DataMappingScenario? _selectedMappingScenario;
        public DataMappingScenario? SelectedMappingScenario
        {
            get => _selectedMappingScenario;
            set { _selectedMappingScenario = value; OnPropertyChanged(); }
        }
        
        // 팝업 창 관련 속성들
        public string MappingScenariosCount => $"{MappingScenarios.Count(s => s.IsEnabled)}";
        public string SerialConnectionStatus => IsConnected ? $"{SerialPort}" : "Disconnected";
        public string ApiEndpointStatus => ApiUrl;
        
        // Monitor 관련 추가 속성들
        private int _serialMessageCount = 0;
        public string SerialMessageCount => _serialMessageCount.ToString();
        
        private int _apiRequestCount = 0;
        public string ApiRequestCount => _apiRequestCount.ToString();
        
        private int _apiSuccessCount = 0;
        public string ApiSuccessRate => _apiRequestCount > 0 ? $"{(_apiSuccessCount * 100 / _apiRequestCount)}%" : "0%";
        
        // Monitor Filters
        public List<string> SerialMonitorFilters { get; } = new() { "All", "Data", "Errors", "Commands" };
        
        private string _serialMonitorFilter = "All";
        public string SerialMonitorFilter
        {
            get => _serialMonitorFilter;
            set { _serialMonitorFilter = value; OnPropertyChanged(); }
        }
        
        private bool _serialShowTimestamps = true;
        public bool SerialShowTimestamps
        {
            get => _serialShowTimestamps;
            set { _serialShowTimestamps = value; OnPropertyChanged(); }
        }
        
        private bool _apiShowHeaders = false;
        public bool ApiShowHeaders
        {
            get => _apiShowHeaders;
            set { _apiShowHeaders = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand TestApiCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand OpenSerialConfigCommand { get; }
        public ICommand SetTransmissionIntervalCommand { get; }
        public ICommand SetBatchSizeCommand { get; }
        public ICommand SetDeviceIdCommand { get; }
        public ICommand AddMappingScenarioCommand { get; }
        public ICommand DeleteMappingScenarioCommand { get; }
        public ICommand TestMappingCommand { get; }
        public ICommand SaveMappingCommand { get; }
        public ICommand ShowReservedWordsCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }
        
        // 터미널 모니터 Commands (중복 제거)
        public ICommand ToggleSerialMonitorCommand { get; }
        public ICommand ToggleApiMonitorCommand { get; }
        public ICommand SaveSerialMonitorCommand { get; }
        public ICommand SaveApiMonitorCommand { get; }
        public ICommand ShowSerialMonitorCommand { get; }
        public ICommand HideSerialMonitorCommand { get; }
        public ICommand ShowApiMonitorCommand { get; }
        public ICommand HideApiMonitorCommand { get; }
        public ICommand ClearSerialMonitorCommand { get; }
        public ICommand ClearApiMonitorCommand { get; }
        
        // 팝업 창 관련 명령들
        public ICommand OpenDataMappingCommand { get; }
        public ICommand OpenSerialMonitorCommand { get; }
        public ICommand OpenApiMonitorCommand { get; }
        
        // 시뮬레이션 관련 명령들
        public ICommand StartSimulationCommand { get; }
        public ICommand StopSimulationCommand { get; }
        public ICommand GenerateSingleDataCommand { get; }
        
        // Queue 및 로그 클리어 명령들
        public ICommand ClearQueueCommand { get; }
        public ICommand ClearLogsCommand { get; }

        // Command Methods
        private async void Connect()
        {
            try
            {
                Status = "Connecting...";
                var success = await _serialService.ConnectAsync();
                IsConnected = success;
                Status = success ? "Connected" : "Connection Failed";
                
                // Queue 상태 업데이트
                UpdateQueueCount();
                
                // 연결 성공 시 자동으로 타이머 시작
                if (success)
                {
                    StartTimerAutomatically();
                }
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
                // 연결 해제 시 자동으로 타이머 중지
                StopTimerAutomatically();
                
                await _serialService.DisconnectAsync();
                IsConnected = false;
                Status = "Disconnected";
                
                // Queue 상태 업데이트
                UpdateQueueCount();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from serial port");
            }
        }

        private void StartTimerAutomatically()
        {
            // 설정된 전송 간격 사용
            if (int.TryParse(TransmissionInterval, out int interval) && interval > 0)
            {
                _timerService.Start(interval);
            }
            else
            {
                _timerService.Start(5); // 기본값 5초
            }
            IsTimerRunning = true;
        }

        private void StopTimerAutomatically()
        {
            _timerService.Stop();
            IsTimerRunning = false;
        }

        /// <summary>
        /// Tests API connection using the test URL (not the mapping table URLs)
        /// 테스트 URL을 사용한 API 연결 테스트 (매핑 테이블 URL과 무관)
        /// </summary>
        private async void TestApi()
        {
            try
            {
                Status = "Testing API...";
                
                // API 테스트 요청 로그 (테스트 전용 URL 사용)
                var requestId = _apiMonitorService.LogApiRequest("GET", _apiUrl, "API Connection Test");
                
                var success = await _httpService.TestConnectionAsync();
                
                // API 테스트 응답 로그
                var statusCode = success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError;
                _apiMonitorService.LogApiResponse(requestId, statusCode, success ? "Connection OK" : "Connection Failed");
                
                Status = success ? "API Connection OK" : "API Connection Failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                Status = "API Test Error";
                
                // API 오류 로그
                _apiMonitorService.LogApiError("test", ex);
            }
        }

        // Command Can Execute
        private bool CanConnect() => !IsConnected;
        private bool CanDisconnect() => IsConnected;

        // Event Handlers
        private async void OnSerialDataReceived(object? sender, Models.SerialDataReceivedEventArgs e)
        {
            var dataString = System.Text.Encoding.UTF8.GetString(e.Data);
            
            // 시리얼 모니터에 수신 데이터 기록
            _serialMonitorService.LogSerialReceived(dataString);
            
            // 데이터 매핑 처리
            await Task.Run(async () =>
            {
                await _dataMappingService.ProcessDataAsync(dataString, DataSource.Serial);
            });
            
            // STX/ETX 기반 파싱하여 큐에 추가
            _queueService.ParseAndEnqueue(e.Data);
            UpdateQueueCount();
            Status = $"Data received. Queue: {QueueCount}";
        }

        private void OnConnectionStatusChanged(object? sender, Models.SerialConnectionEventArgs e)
        {
            IsConnected = e.IsConnected;
            Status = e.Message;
        }

        private void OnQueueProcessed(object? sender, EventArgs e)
        {
            // UI 스레드에서 QueueCount 업데이트
            System.Windows.Application.Current.Dispatcher.BeginInvoke(UpdateQueueCount);
        }
        
        private async void OnMappingProcessed(object? sender, MappingProcessedEventArgs e)
        {
            // 매핑 결과를 시리얼 모니터에 기록
            _serialMonitorService.LogMappingResult(e.OriginalData, e.Result.ProcessedData, e.Scenario.Name);
            
            // 전송 타입에 따른 처리
            if (e.Result.Success)
            {
                if (e.Result.TransmissionType == TransmissionType.Api)
                {
                    await ProcessApiTransmission(e.Result, e.Scenario);
                }
                else if (e.Result.TransmissionType == TransmissionType.Serial)
                {
                    await ProcessSerialTransmission(e.Result, e.Scenario);
                }
            }
            
            // UI 스레드에서 상태 업데이트
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Status = $"Mapped: {e.Scenario.Name}";
            });
        }

        /// <summary>
        /// API 전송 처리 - 새로운 큐 시스템 사용
        /// </summary>
        private async Task ProcessApiTransmission(Services.MappingResult result, DataMappingScenario scenario)
        {
            try
            {
                // API 데이터 큐에 메시지 추가 - JSON 변환하지 않고 원본 데이터 사용
                var apiData = new MappedApiData
                {
                    EndpointName = "default", // 기본 엔드포인트 사용
                    ApiEndpoint = GetApiEndpointForScenario(scenario, result.ProcessedData), // FullPath 지원
                    ApiMethod = scenario.ApiMethod ?? "POST", // 시나리오의 HTTP 메서드 사용
                    ContentType = scenario.ContentType ?? "application/json", // 시나리오의 ContentType 사용
                    Payload = new Dictionary<string, object> 
                    { 
                        { "data", result.ProcessedData },
                        { "originalData", result.OriginalData } // 원본 데이터도 보존
                    },
                    CreatedAt = DateTime.Now,
                    MessageId = Guid.NewGuid().ToString(),
                    Priority = 5,
                    RetryCount = 0,
                    MaxRetries = 3,
                    // 원본 데이터 보존을 위한 ParsedData 생성
                    OriginalParsedData = new ParsedData
                    {
                        DeviceId = "device001", // 기본 디바이스 ID
                        DataSource = "serial",
                        Timestamp = DateTime.Now,
                        OriginalData = new RawSerialData(
                            System.Text.Encoding.UTF8.GetBytes(result.OriginalData),
                            "TEXT",
                            "device001",
                            "COM1"
                        )
                    }
                };

                // QueueMessage로 래핑
                var queueMessage = new QueueMessage<MappedApiData>
                {
                    MessageId = apiData.MessageId,
                    Payload = apiData,
                    Priority = apiData.Priority,
                    EnqueueTime = DateTime.UtcNow,
                    Status = MessageStatus.Queued,
                    RetryCount = 0
                };

                var queue = _queueManager.GetQueue<MappedApiData>("ApiDataQueue");
                if (queue != null)
                {
                    await queue.EnqueueAsync(queueMessage);
                    
                    // API 모니터에 큐에 추가됨을 로그
                    var requestId = _apiMonitorService.LogApiRequest(result.ApiMethod, 
                        scenario.ApiEndpoint ?? _apiUrl, result.ProcessedData);
                    _apiMonitorService.LogApiResponse(requestId, System.Net.HttpStatusCode.Accepted, 
                        "Queued for processing", null, 0);
                }
                else
                {
                    _logger.LogError("API data queue not found - falling back to direct transmission");
                    await ProcessApiTransmissionFallback(result, scenario);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing API data for scenario '{ScenarioName}'", scenario.Name);
                await ProcessApiTransmissionFallback(result, scenario);
            }
        }

        /// <summary>
        /// API 전송 처리 - 기존 방식 (폴백용)
        /// </summary>
        private async Task ProcessApiTransmissionFallback(Services.MappingResult result, DataMappingScenario scenario)
        {
            try
            {
                // API 모니터에 요청 시작 로그
                var requestId = _apiMonitorService.LogApiRequest(result.ApiMethod, result.ApiEndpoint, result.ProcessedData);

                // 매핑 테이블의 설정을 우선 사용하여 API URL 결정 (FullPath 지원)
                var apiUrl = GetApiEndpointForScenario(scenario, result.ProcessedData);
                
                // HTTP 서비스에 임시 URL 설정
                var originalUrl = _apiUrl;
                _httpService.SetApiUrl(apiUrl);
                
                // API 호출 수행 (JSON 형태로 전송)
                var startTime = DateTime.Now;
                bool success = await _httpService.SendJsonAsync(result.ProcessedData);
                var responseTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                
                // 원래 URL 복원 (테스트용 URL로 복원)
                _httpService.SetApiUrl(originalUrl);
                
                // API 모니터에 결과 로그
                if (success)
                {
                    _apiMonitorService.LogApiResponse(requestId, System.Net.HttpStatusCode.OK, 
                        "Data transmitted successfully", null, responseTime);
                }
                else
                {
                    _apiMonitorService.LogApiResponse(requestId, System.Net.HttpStatusCode.BadRequest, 
                        "API transmission failed", null, responseTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API transmission for scenario '{ScenarioName}'", scenario.Name);
                _apiMonitorService.LogApiError(_apiMonitorService.LogApiRequest(result.ApiMethod, result.ApiEndpoint), ex);
            }
        }

        /// <summary>
        /// Serial 전송 처리 - 처리된 데이터를 시리얼 포트로 전송
        /// </summary>
        private async Task ProcessSerialTransmission(Services.MappingResult result, DataMappingScenario scenario)
        {
            try
            {
                
                if (!_serialService.IsConnected)
                {
                    _logger.LogWarning("Cannot transmit serial data - not connected to serial port");
                    return;
                }

                string dataToSend = result.ProcessedData?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(dataToSend))
                {
                    _logger.LogWarning("No data to transmit for scenario '{ScenarioName}'", scenario.Name);
                    return;
                }

                // 시리얼 데이터 전송
                bool success = await _serialService.SendTextAsync(dataToSend);
                
                if (success)
                {
                    // 전송된 데이터를 시리얼 모니터에 TX로 기록
                    _serialMonitorService.LogSerialSent(dataToSend);
                }
                else
                {
                    _logger.LogError("Serial transmission failed for scenario '{ScenarioName}'", scenario.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during serial transmission for scenario '{ScenarioName}'", scenario.Name);
            }
        }

        /// <summary>
        /// 시리얼 모니터 메시지 추가 이벤트 핸들러
        /// </summary>
        private void OnSerialMonitorMessageAdded(object? sender, MonitorMessage message)
        {
            // UI 스레드에서 텍스트 업데이트
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                SerialMonitorText += message.FormattedMessage + Environment.NewLine;
                
                // 메시지 카운트 업데이트
                _serialMessageCount++;
                OnPropertyChanged(nameof(SerialMessageCount));
            });
        }

        /// <summary>
        /// API 모니터 메시지 추가 이벤트 핸들러
        /// </summary>
        private void OnApiMonitorMessageAdded(object? sender, ApiMonitorMessage message)
        {
            // UI 스레드에서 텍스트 업데이트
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ApiMonitorText += message.FormattedMessage + Environment.NewLine;
                
                // API 카운트 업데이트
                if (message.IsCompleted)
                {
                    _apiRequestCount++;
                    if (message.StatusCode.HasValue && 
                        (int)message.StatusCode.Value >= 200 && (int)message.StatusCode.Value < 300)
                    {
                        _apiSuccessCount++;
                    }
                    
                    OnPropertyChanged(nameof(ApiRequestCount));
                    OnPropertyChanged(nameof(ApiSuccessRate));
                }
            });
        }

        // COM Port Management Methods
        private void RefreshPorts()
        {
            try
            {
                var portsWithDescriptions = _comPortDiscovery.GetAvailablePortsWithDescriptions();
                var currentSelectedPort = SerialPort;
                
                AvailablePorts.Clear();

                foreach (var port in portsWithDescriptions)
                {
                    var portInfo = new ComPortInfo
                    {
                        PortName = port.Key,
                        Description = port.Value
                    };
                    AvailablePorts.Add(portInfo);
                }

                
                // 새로고침 후 스마트 선택 수행 (기존 선택이 없거나 더 이상 사용할 수 없는 경우)
                if (string.IsNullOrEmpty(currentSelectedPort) || 
                    !AvailablePorts.Any(p => p.PortName == currentSelectedPort))
                {
                    PerformSmartSelection();
                }
                else
                {
                    // 기존 선택된 포트가 여전히 사용 가능하면 유지
                    SerialPort = currentSelectedPort;
                    Status = $"Port refreshed. Current: {currentSelectedPort}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing COM ports");
                Status = "Error refreshing COM ports";
            }
        }

        private void InitializeSmartPortSelection()
        {
            PerformSmartSelection();
        }

        private void PerformSmartSelection()
        {
            try
            {
                var smartPort = _comPortDiscovery.GetBestAvailableComPort();
                if (!string.IsNullOrEmpty(smartPort))
                {
                    SerialPort = smartPort;
                    
                    // 모든 포트의 선택 상태 초기화
                    foreach (var port in AvailablePorts)
                    {
                        port.IsSmartSelected = false;
                        port.IsLastUsed = false;
                    }
                    
                    // 스마트 선택된 포트 표시
                    var selectedPortInfo = AvailablePorts.FirstOrDefault(p => p.PortName == smartPort);
                    if (selectedPortInfo != null)
                    {
                        selectedPortInfo.IsSmartSelected = true;
                        
                        // 마지막 사용 포트인지 확인
                        var lastUsedPort = System.Configuration.ConfigurationManager.AppSettings["LastUsedComPort"];
                        if (smartPort == lastUsedPort)
                        {
                            selectedPortInfo.IsLastUsed = true;
                            Status = $"Last used port selected: {smartPort}";
                        }
                        else
                        {
                            Status = $"Smart selected: {smartPort}";
                        }
                    }
                    
                }
                else
                {
                    Status = "No COM ports available";
                    _logger.LogWarning("No COM ports available for smart selection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart port selection");
                Status = "Error in smart port selection";
            }
        }

        /// <summary>
        /// Checks and performs auto-connect if enabled
        /// </summary>
        private async Task CheckAutoConnect()
        {
            try
            {
                // 초기화 완료를 위해 잠시 대기
                await Task.Delay(2000);
                
                var (enabled, portName) = _comPortDiscovery.GetAutoConnectSettings();
                
                if (enabled && !string.IsNullOrEmpty(portName))
                {
                    // UI 스레드에서 실행
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        if (!IsConnected && AvailablePorts.Any(p => p.PortName == portName))
                        {
                            SerialPort = portName;
                            Status = $"Auto-connecting to {portName}...";
                            
                            // 잠시 대기 후 연결 시도
                            await Task.Delay(1000);
                            Connect();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-connect");
            }
        }

        /// <summary>
        /// Sets the transmission interval setting
        /// </summary>
        private void SetTransmissionInterval()
        {
            try
            {
                if (int.TryParse(TransmissionInterval, out int interval) && interval > 0)
                {
                    // App.config에 설정 저장
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                        System.Configuration.ConfigurationUserLevel.None);
                    
                    config.AppSettings.Settings["QueueTransmissionInterval"].Value = TransmissionInterval;
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                    
                    Status = $"Transmission interval set to {interval} seconds";
                }
                else
                {
                    Status = "Invalid transmission interval. Must be a positive number.";
                    _logger.LogWarning("Invalid transmission interval entered: {Input}", TransmissionInterval);
                }
            }
            catch (Exception ex)
            {
                Status = "Error setting transmission interval";
                _logger.LogError(ex, "Error setting transmission interval");
            }
        }
        
        /// <summary>
        /// Sets the batch size setting
        /// </summary>
        private void SetBatchSize()
        {
            try
            {
                if (int.TryParse(BatchSize, out int batchSize) && batchSize > 0)
                {
                    // App.config에 설정 저장
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                        System.Configuration.ConfigurationUserLevel.None);
                    
                    config.AppSettings.Settings["QueueBatchSize"].Value = BatchSize;
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                    
                    Status = $"Batch size set to {batchSize}";
                }
                else
                {
                    Status = "Invalid batch size. Must be a positive number.";
                    _logger.LogWarning("Invalid batch size entered: {Input}", BatchSize);
                }
            }
            catch (Exception ex)
            {
                Status = "Error setting batch size";
                _logger.LogError(ex, "Error setting batch size");
            }
        }

        /// <summary>
        /// Loads API URL from configuration (for connection testing only)
        /// 설정에서 연결 테스트용 API URL 로드
        /// </summary>
        private void LoadApiUrl()
        {
            try
            {
                // 1. ConfigurationService의 API endpoints에서 default 또는 첫 번째 엔드포인트 가져오기 (테스트 용도)
                var config = _configurationService.ApplicationConfig;
                if (config.ApiEndpoints != null && config.ApiEndpoints.Any())
                {
                    var defaultEndpoint = config.ApiEndpoints.FirstOrDefault(e => e.Name.Equals("default", StringComparison.OrdinalIgnoreCase))
                                        ?? config.ApiEndpoints.First();
                    
                    _apiUrl = defaultEndpoint.Url;
                    return;
                }

                // 2. 레거시 AppSettings에서 가져오기
                var legacyApiUrl = System.Configuration.ConfigurationManager.AppSettings["ApiEndpoint"];
                if (!string.IsNullOrEmpty(legacyApiUrl))
                {
                    _apiUrl = legacyApiUrl;
                    return;
                }

                // 3. 기본값 유지
                _logger.LogWarning("No API URL found in configuration, using default: {ApiUrl}", _apiUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading API URL from configuration, using default: {ApiUrl}", _apiUrl);
            }
        }

        /// <summary>
        /// Loads queue settings from App.config
        /// </summary>
        private void LoadQueueSettings()
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
                // 기본값 사용
                TransmissionInterval = "5";
                BatchSize = "10";
                DeviceId = "";
            }
        }

        /// <summary>
        /// Initializes mapping scenarios from the data mapping service
        /// </summary>
        private void InitializeMappingScenarios()
        {
            try
            {
                // 데이터 매핑 서비스의 시나리오들을 UI 컬렉션에 동기화
                MappingScenarios.Clear();
                
                foreach (var scenario in _dataMappingService.Scenarios)
                {
                    MappingScenarios.Add(scenario);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing mapping scenarios");
            }
        }

        /// <summary>
        /// Updates the current queue count from the queue service
        /// </summary>
        private void UpdateQueueCount()
        {
            try
            {
                QueueCount = _queueService.Count;
                _logger.LogDebug("Queue count updated: {Count}", QueueCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue count");
                QueueCount = 0; // 오류 시 0으로 설정
            }
        }

        /// <summary>
        /// Sets the device ID setting
        /// </summary>
        private void SetDeviceId()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(DeviceId))
                {
                    // App.config에 설정 저장
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                        System.Configuration.ConfigurationUserLevel.None);
                    
                    config.AppSettings.Settings["DeviceId"].Value = DeviceId;
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                    
                    Status = $"Device ID set to '{DeviceId}'";
                }
                else
                {
                    Status = "Device ID cannot be empty";
                    _logger.LogWarning("Attempted to set empty Device ID");
                }
            }
            catch (Exception ex)
            {
                Status = "Error setting Device ID";
                _logger.LogError(ex, "Error setting Device ID");
            }
        }

        /// <summary>
        /// Adds a new mapping scenario
        /// </summary>
        private void AddMappingScenario()
        {
            try
            {
                if (MappingScenarios.Count >= 10)
                {
                    Status = "Maximum 10 scenarios allowed";
                    return;
                }

                var newScenario = new DataMappingScenario
                {
                    IsEnabled = true,
                    Name = $"Scenario {MappingScenarios.Count + 1}",
                    Source = DataSource.Serial,
                    Identifier = "",
                    ValueTemplate = "@deviceId|@yyyyMMddHHmmss|{data}",
                    TransmissionType = TransmissionType.Api,
                    ApiMethod = "POST",
                    ApiEndpoint = "/data",
                    UseFullPath = false,
                    FullPathTemplate = "http://diveinto.space:54321/api/qr_bypasser.aspx?dn=@deviceId&br={data}"
                };

                MappingScenarios.Add(newScenario);
                SelectedMappingScenario = newScenario;
                
                Status = $"Added new scenario: {newScenario.Name}";
            }
            catch (Exception ex)
            {
                Status = "Error adding scenario";
                _logger.LogError(ex, "Error adding mapping scenario");
            }
        }

        /// <summary>
        /// Deletes the selected mapping scenario
        /// </summary>
        private void DeleteMappingScenario()
        {
            try
            {
                if (SelectedMappingScenario != null)
                {
                    var scenarioName = SelectedMappingScenario.Name;
                    MappingScenarios.Remove(SelectedMappingScenario);
                    SelectedMappingScenario = null;
                    
                    Status = $"Deleted scenario: {scenarioName}";
                }
                else
                {
                    Status = "No scenario selected for deletion";
                }
            }
            catch (Exception ex)
            {
                Status = "Error deleting scenario";
                _logger.LogError(ex, "Error deleting mapping scenario");
            }
        }

        /// <summary>
        /// Tests the mapping scenarios
        /// </summary>
        private async void TestMapping()
        {
            try
            {
                var enabledScenarios = MappingScenarios.Where(s => s.IsEnabled).ToList();
                Status = $"Found {enabledScenarios.Count} enabled scenarios ready for testing";
                
                // 테스트 데이터로 매핑 테스트
                var testData = "STX TEST DATA ETX";
                var results = await _dataMappingService.ProcessDataAsync(testData, DataSource.Serial);
                
                if (results.Any())
                {
                    Status = $"Test successful: {results.Count} matches found";
                }
                else
                {
                    Status = "Test completed: No matches found";
                }
                
            }
            catch (Exception ex)
            {
                Status = "Error testing mapping";
                _logger.LogError(ex, "Error testing mapping scenarios");
            }
        }

        /// <summary>
        /// Saves the current mapping scenarios
        /// </summary>
        private void SaveMapping()
        {
            try
            {
                // 매핑 시나리오를 데이터 매핑 서비스에 저장
                _dataMappingService.ClearScenarios();
                foreach (var scenario in MappingScenarios)
                {
                    _dataMappingService.AddScenario(scenario);
                }
                
                // 파일에 저장
                _dataMappingService.SaveScenariosToFile();
                
                Status = $"Saved {MappingScenarios.Count} mapping scenarios to file";
            }
            catch (Exception ex)
            {
                Status = "Error saving mapping scenarios";
                _logger.LogError(ex, "Error saving mapping scenarios");
            }
        }

        /// <summary>
        /// Data mapping window close requested event
        /// </summary>
        public event EventHandler<bool>? DataMappingWindowCloseRequested;

        /// <summary>
        /// Applies data mapping changes and closes window
        /// </summary>
        private void ApplyDataMapping()
        {
            try
            {
                // 매핑 저장
                SaveMapping();
                
                // 창 닫기 요청 (저장 성공)
                DataMappingWindowCloseRequested?.Invoke(this, true);
                
                Status = "Data mapping changes applied and saved";
            }
            catch (Exception ex)
            {
                Status = "Error applying data mapping changes";
                _logger.LogError(ex, "Error applying data mapping changes");
            }
        }

        /// <summary>
        /// Cancels data mapping changes
        /// </summary>
        private void CancelDataMapping()
        {
            try
            {
                // 창 닫기 요청 (저장하지 않음)
                DataMappingWindowCloseRequested?.Invoke(this, false);
                
                Status = "Data mapping window closed without saving";
            }
            catch (Exception ex)
            {
                Status = "Error closing data mapping window";
                _logger.LogError(ex, "Error closing data mapping window");
            }
        }

        /// <summary>
        /// Shows the serial monitor
        /// </summary>
        private void ShowSerialMonitor()
        {
            SerialMonitorVisible = true;
        }

        /// <summary>
        /// Hides the serial monitor
        /// </summary>
        private void HideSerialMonitor()
        {
            SerialMonitorVisible = false;
        }

        /// <summary>
        /// Shows the API monitor
        /// </summary>
        private void ShowApiMonitor()
        {
            ApiMonitorVisible = true;
        }

        /// <summary>
        /// Hides the API monitor
        /// </summary>
        private void HideApiMonitor()
        {
            ApiMonitorVisible = false;
        }

        /// <summary>
        /// Clears the serial monitor
        /// </summary>
        private void ClearSerialMonitor()
        {
            _serialMonitorService.Clear();
            SerialMonitorText = string.Empty;
            _serialMessageCount = 0;
            OnPropertyChanged(nameof(SerialMessageCount));
            Status = "Serial monitor cleared";
        }

        /// <summary>
        /// Clears the API monitor
        /// </summary>
        private void ClearApiMonitor()
        {
            _apiMonitorService.Clear();
            ApiMonitorText = string.Empty;
            _apiRequestCount = 0;
            _apiSuccessCount = 0;
            OnPropertyChanged(nameof(ApiRequestCount));
            OnPropertyChanged(nameof(ApiSuccessRate));
            Status = "API monitor cleared";
        }

        /// <summary>
        /// Toggles the serial monitor visibility
        /// </summary>
        private void ToggleSerialMonitor()
        {
            SerialMonitorVisible = !SerialMonitorVisible;
            var action = SerialMonitorVisible ? "shown" : "hidden";
            Status = $"Serial monitor {action}";
        }

        /// <summary>
        /// Toggles the API monitor visibility
        /// </summary>
        private void ToggleApiMonitor()
        {
            ApiMonitorVisible = !ApiMonitorVisible;
            var action = ApiMonitorVisible ? "shown" : "hidden";
            Status = $"API monitor {action}";
        }

        /// <summary>
        /// Saves serial monitor content to file
        /// </summary>
        private void SaveSerialMonitor()
        {
            try
            {
                var fileName = $"SerialMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                System.IO.File.WriteAllText(filePath, SerialMonitorText);
                Status = $"Serial monitor saved to {fileName}";
            }
            catch (Exception ex)
            {
                Status = "Error saving serial monitor";
                _logger.LogError(ex, "Error saving serial monitor");
            }
        }

        /// <summary>
        /// Saves API monitor content to file
        /// </summary>
        private void SaveApiMonitor()
        {
            try
            {
                var fileName = $"ApiMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                System.IO.File.WriteAllText(filePath, ApiMonitorText);
                Status = $"API monitor saved to {fileName}";
            }
            catch (Exception ex)
            {
                Status = "Error saving API monitor";
                _logger.LogError(ex, "Error saving API monitor");
            }
        }

        /// <summary>
        /// Opens the Data Mapping configuration window
        /// </summary>
        private void OpenDataMapping()
        {
            try
            {
                // 이미 열려있는 창이 있는지 확인 (null이 아니고 닫히지 않은 상태)
                if (_dataMappingWindow != null)
                {
                    try
                    {
                        // 창이 실제로 사용 가능한지 확인
                        if (_dataMappingWindow.IsVisible)
                        {
                            // 기존 창을 활성화하고 포커스 설정
                            _dataMappingWindow.Activate();
                            _dataMappingWindow.Focus();
                            Status = "Data mapping window focused";
                            return;
                        }
                        else
                        {
                            // 창이 존재하지만 보이지 않는 경우 (닫힌 상태) - 참조 정리
                            _dataMappingWindow = null;
                        }
                    }
                    catch (Exception)
                    {
                        // 창 객체가 이미 해제된 경우 - 참조 정리
                        _dataMappingWindow = null;
                    }
                }

                // 새 창 생성
                _dataMappingWindow = new Views.DataMappingWindow(this);
                
                // 창이 닫힐 때 참조를 null로 설정
                _dataMappingWindow.Closed += (sender, e) => 
                {
                    _dataMappingWindow = null;
                };
                
                _dataMappingWindow.Show(); // ShowDialog() 대신 Show() 사용하여 모달이 아닌 일반 창으로 열기
                OnPropertyChanged(nameof(MappingScenariosCount));
                Status = "Data mapping window opened";
            }
            catch (Exception ex)
            {
                Status = "Error opening data mapping window";
                _logger.LogError(ex, "Error opening data mapping window");
            }
        }

        /// <summary>
        /// Shows the reserved words information window
        /// </summary>
        private void ShowReservedWords()
        {
            try
            {
                // 이미 열려있는 창이 있는지 확인 (null이 아니고 닫히지 않은 상태)
                if (_reservedWordsWindow != null)
                {
                    try
                    {
                        // 창이 실제로 사용 가능한지 확인
                        if (_reservedWordsWindow.IsVisible)
                        {
                            // 기존 창을 활성화하고 포커스 설정
                            _reservedWordsWindow.Activate();
                            _reservedWordsWindow.Focus();
                            Status = "Reserved words window focused";
                            return;
                        }
                        else
                        {
                            // 창이 존재하지만 보이지 않는 경우 (닫힌 상태) - 참조 정리
                            _reservedWordsWindow = null;
                        }
                    }
                    catch (Exception)
                    {
                        // 창 객체가 이미 해제된 경우 - 참조 정리
                        _reservedWordsWindow = null;
                    }
                }

                // 새 창 생성
                _reservedWordsWindow = new Views.ReservedWordsWindow();
                
                // 창이 닫힐 때 참조를 null로 설정
                _reservedWordsWindow.Closed += (sender, e) => 
                {
                    _reservedWordsWindow = null;
                };
                
                _reservedWordsWindow.Show(); // ShowDialog() 대신 Show() 사용하여 모달이 아닌 일반 창으로 열기
                Status = "Reserved words window shown";
            }
            catch (Exception ex)
            {
                Status = "Error showing reserved words window";
                _logger.LogError(ex, "Error showing reserved words window");
            }
        }

        /// <summary>
        /// Gets the API endpoint for a scenario, supporting FullPath with reserved word replacement
        /// </summary>
        /// <param name="scenario">Data mapping scenario</param>
        /// <param name="data">Data to substitute in templates</param>
        /// <returns>Final API endpoint URL</returns>
        private string GetApiEndpointForScenario(DataMappingScenario scenario, string data)
        {
            try
            {
                if (scenario.UseFullPath && !string.IsNullOrEmpty(scenario.FullPathTemplate))
                {
                    // Use FullPath template with reserved word replacement
                    var finalUrl = scenario.FullPathTemplate;
                    
                    // First replace reserved words
                    finalUrl = _reservedWordService.ProcessReservedWords(finalUrl);
                    
                    // Then replace {data} placeholder
                    finalUrl = finalUrl.Replace("{data}", data);
                    
                    _logger.LogDebug("Built full path URL for scenario '{ScenarioName}': {Template} -> {FinalUrl}", 
                        scenario.Name, scenario.FullPathTemplate, finalUrl);
                    
                    return finalUrl;
                }
                else
                {
                    // Use standard API endpoint
                    return scenario.ApiEndpoint ?? "/";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building API endpoint for scenario '{ScenarioName}'", scenario.Name);
                return scenario.ApiEndpoint ?? "/"; // Fallback to original endpoint
            }
        }

        /// <summary>
        /// Opens the Serial Monitor window
        /// </summary>
        private void OpenSerialMonitor()
        {
            try
            {
                // 이미 열려있는 창이 있는지 확인 (null이 아니고 닫히지 않은 상태)
                if (_serialMonitorWindow != null)
                {
                    try
                    {
                        // 창이 실제로 사용 가능한지 확인
                        if (_serialMonitorWindow.IsVisible)
                        {
                            // 기존 창을 활성화하고 포커스 설정
                            _serialMonitorWindow.Activate();
                            _serialMonitorWindow.Focus();
                            Status = "Serial monitor window focused";
                            return;
                        }
                        else
                        {
                            // 창이 존재하지만 보이지 않는 경우 (닫힌 상태) - 참조 정리
                            _serialMonitorWindow = null;
                        }
                    }
                    catch (Exception)
                    {
                        // 창 객체가 이미 해제된 경우 - 참조 정리
                        _serialMonitorWindow = null;
                    }
                }

                // 기존 메시지들을 텍스트로 로드
                LoadExistingSerialMessages();
                
                // 새 창 생성
                _serialMonitorWindow = new Views.SerialMonitorWindow(this);
                
                // 창이 닫힐 때 참조를 null로 설정
                _serialMonitorWindow.Closed += (sender, e) => 
                {
                    _serialMonitorWindow = null;
                };
                
                _serialMonitorWindow.Show();
                Status = "Serial monitor window opened";
            }
            catch (Exception ex)
            {
                Status = "Error opening serial monitor window";
                _logger.LogError(ex, "Error opening serial monitor window");
            }
        }

        /// <summary>
        /// Opens the API Monitor window
        /// </summary>
        private void OpenApiMonitor()
        {
            try
            {
                // 이미 열려있는 창이 있는지 확인 (null이 아니고 닫히지 않은 상태)
                if (_apiMonitorWindow != null)
                {
                    try
                    {
                        // 창이 실제로 사용 가능한지 확인
                        if (_apiMonitorWindow.IsVisible)
                        {
                            // 기존 창을 활성화하고 포커스 설정
                            _apiMonitorWindow.Activate();
                            _apiMonitorWindow.Focus();
                            Status = "API monitor window focused";
                            return;
                        }
                        else
                        {
                            // 창이 존재하지만 보이지 않는 경우 (닫힌 상태) - 참조 정리
                            _apiMonitorWindow = null;
                        }
                    }
                    catch (Exception)
                    {
                        // 창 객체가 이미 해제된 경우 - 참조 정리
                        _apiMonitorWindow = null;
                    }
                }

                // 기존 메시지들을 텍스트로 로드
                LoadExistingApiMessages();
                
                // 새 창 생성
                _apiMonitorWindow = new Views.ApiMonitorWindow(this);
                
                // 창이 닫힐 때 참조를 null로 설정
                _apiMonitorWindow.Closed += (sender, e) => 
                {
                    _apiMonitorWindow = null;
                };
                
                _apiMonitorWindow.Show();
                Status = "API monitor window opened";
            }
            catch (Exception ex)
            {
                Status = "Error opening API monitor window";
                _logger.LogError(ex, "Error opening API monitor window");
            }
        }

        /// <summary>
        /// Opens the Serial Configuration window
        /// </summary>
        private async void OpenSerialConfig()
        {
            try
            {
                // ConfigurationService에서 최신 설정을 가져옴
                var currentSettings = _configurationService.ApplicationConfig.SerialSettings;
                
                var window = new Views.SerialConfigWindow(currentSettings);
                var result = window.ShowDialog();
                
                if (result == true && window.IsChanged)
                {
                    // 설정을 ConfigurationService에 저장
                    _configurationService.SaveSerialSettings(window.Settings);
                    
                    // SerialCommunicationService의 ConnectionSettings 전체 업데이트
                    _serialService.UpdateConnectionSettings(window.Settings);
                    
                    // 설정이 변경되었으므로 연결이 되어있다면 재연결
                    if (IsConnected)
                    {
                        Status = "Serial configuration changed, reconnecting...";
                        Disconnect();
                        await Task.Delay(500); // 잠시 대기
                        Connect();
                    }
                    Status = "Serial configuration updated and saved";
                }
            }
            catch (Exception ex)
            {
                Status = "Error opening serial configuration window";
                _logger.LogError(ex, "Error opening serial configuration window");
            }
        }

        /// <summary>
        /// 기존 시리얼 메시지들을 텍스트에 로드
        /// </summary>
        private void LoadExistingSerialMessages()
        {
            try
            {
                var messages = _serialMonitorService.Messages;
                // null 체크를 추가하여 NullReferenceException 방지
                SerialMonitorText = string.Join(Environment.NewLine, 
                    messages.Where(m => m?.FormattedMessage != null)
                           .Select(m => m.FormattedMessage));
                if (!string.IsNullOrEmpty(SerialMonitorText))
                {
                    SerialMonitorText += Environment.NewLine;
                }
                _serialMessageCount = messages.Count;
                OnPropertyChanged(nameof(SerialMessageCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading existing serial messages");
                SerialMonitorText = "Error loading messages: " + ex.Message;
            }
        }

        /// <summary>
        /// 기존 API 메시지들을 텍스트에 로드
        /// </summary>
        private void LoadExistingApiMessages()
        {
            var messages = _apiMonitorService.Messages;
            ApiMonitorText = string.Join(Environment.NewLine, messages.Select(m => m.FormattedMessage));
            if (!string.IsNullOrEmpty(ApiMonitorText))
            {
                ApiMonitorText += Environment.NewLine;
            }
            
            _apiRequestCount = messages.Count;
            _apiSuccessCount = messages.Count(m => m.StatusCode.HasValue && 
                (int)m.StatusCode.Value >= 200 && (int)m.StatusCode.Value < 300);
            
            OnPropertyChanged(nameof(ApiRequestCount));
            OnPropertyChanged(nameof(ApiSuccessRate));
        }

        // 시뮬레이션 관련 메서드들
        
        /// <summary>
        /// 시뮬레이션 토글 (시작/중지)
        /// </summary>
        private void ToggleSimulation()
        {
            if (IsSimulating)
            {
                StopSimulation();
            }
            else
            {
                StartSimulation();
            }
        }

        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        private void StartSimulation()
        {
            try
            {
                if (int.TryParse(SimulationInterval, out var interval) && interval > 0)
                {
                    _serialDataSimulator.Start(interval);
                    IsSimulating = true;
                    Status = $"Simulation started (interval: {interval}s)";
                }
                else
                {
                    System.Windows.MessageBox.Show("Please enter a valid simulation interval (positive number).", 
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting simulation");
                System.Windows.MessageBox.Show($"Error starting simulation: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 시뮬레이션 중지
        /// </summary>
        private void StopSimulation()
        {
            try
            {
                _serialDataSimulator.Stop();
                IsSimulating = false;
                Status = "Simulation stopped";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping simulation");
                System.Windows.MessageBox.Show($"Error stopping simulation: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 단일 시뮬레이션 데이터 생성
        /// </summary>
        private void GenerateSingleData()
        {
            try
            {
                _serialDataSimulator.GenerateSingleData();
                Status = "Single simulation data generated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating single simulation data");
                System.Windows.MessageBox.Show($"Error generating data: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 시뮬레이션 데이터 수신 이벤트 핸들러
        /// </summary>
        private async void OnSimulatedDataReceived(object? sender, SimulatedSerialDataEventArgs e)
        {
            try
            {
                // 시뮬레이션 데이터를 실제 시리얼 데이터처럼 처리
                var serialEventArgs = new Models.SerialDataReceivedEventArgs(e.Data);
                
                // 시리얼 모니터에 시뮬레이션 데이터 기록
                _serialMonitorService.LogSerialReceived($"[SIM] {e.DataString}");
                
                // 데이터 매핑 처리
                await Task.Run(async () =>
                {
                    await _dataMappingService.ProcessDataAsync(e.DataString, DataSource.Serial);
                });
                
                // STX/ETX 기반 파싱하여 큐에 추가
                _queueService.ParseAndEnqueue(e.Data);
                
                // UI 업데이트
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateQueueCount();
                    Status = $"Simulation data processed. Queue: {QueueCount} (Scenario: {e.Scenario})";
                });
                
                _logger.LogDebug("Processed simulation data: '{Data}' (Scenario: {Scenario})", 
                    e.DataString, e.Scenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simulated data: {Data}", e.DataString);
            }
        }

        /// <summary>
        /// 큐 매니저 초기화 및 API 데이터 처리 시작 (조건부)
        /// </summary>
        private async Task InitializeQueueProcessingConditional()
        {
            try
            {
                // 먼저 테스트 API 호출로 연결 확인
                
                var testSuccess = await TestApiConnectivity();
                if (testSuccess)
                {
                    await InitializeQueueProcessing();
                }
                else
                {
                    _logger.LogWarning("API connectivity test failed - queue processing disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during conditional queue initialization");
            }
        }

        /// <summary>
        /// API 연결성 테스트
        /// </summary>
        private async Task<bool> TestApiConnectivity()
        {
            try
            {
                var testUrl = "http://diveinto.space:54321";
                var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                
                var response = await httpClient.GetAsync(testUrl);
                var success = response != null; // 어떤 응답이든 연결되면 성공
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API connectivity test failed");
                return false;
            }
        }

        /// <summary>
        /// 큐 매니저 초기화 및 API 데이터 처리 시작
        /// </summary>
        private async Task InitializeQueueProcessing()
        {
            try
            {

                // API 데이터 큐 생성
                const string queueName = "ApiDataQueue";
                var queueConfig = new QueueConfiguration
                {
                    MaxSize = 1000,
                    BatchSize = 10,
                    BatchTimeoutMs = 1000,
                    RetryCount = 3,
                    RetryIntervalMs = 5000,
                    EnablePriority = false,
                    ProcessorThreadCount = 1,
                    EnableAsync = true,
                    Name = queueName
                };
                
                var queue = _queueManager.CreateQueue<MappedApiData>(queueName, queueConfig);

                // 큐 프로세서 시작
                var success = await _queueManager.StartProcessingAsync(queueName, _apiDataQueueProcessor);
                
                if (success)
                {
                }
                else
                {
                    _logger.LogError("Failed to start queue processing");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing queue processing");
            }
        }

        /// <summary>
        /// 메시지 큐 클리어
        /// </summary>
        private async void ClearQueue()
        {
            try
            {
                
                // 큐 매니저를 통해 큐 클리어
                var queueNames = _queueManager.GetQueueNames();
                foreach (var queueName in queueNames)
                {
                    await _queueManager.ClearQueueAsync(queueName);
                }
                
                // 레거시 큐 서비스도 클리어
                _queueService.ClearQueue();
                
                // Queue Count 업데이트
                QueueCount = 0;
                
                System.Windows.MessageBox.Show("메시지 큐가 성공적으로 삭제되었습니다.", "Queue 클리어", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing message queue");
                System.Windows.MessageBox.Show($"큐 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 모든 모니터 로그 클리어
        /// </summary>
        private void ClearLogs()
        {
            try
            {
                
                // Serial Monitor 로그 클리어
                _serialMonitorService.ClearLogs();
                
                // API Monitor 로그 클리어
                _apiMonitorService.ClearLogs();
                
                System.Windows.MessageBox.Show("모든 모니터 로그가 성공적으로 삭제되었습니다.", "로그 클리어", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing monitor logs");
                System.Windows.MessageBox.Show($"로그 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 모든 자식 창들을 닫습니다
        /// </summary>
        public void CloseAllChildWindows()
        {
            try
            {

                // 데이터 매핑 창 닫기
                if (_dataMappingWindow != null)
                {
                    try
                    {
                        _dataMappingWindow.Close();
                        _dataMappingWindow = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing data mapping window");
                        _dataMappingWindow = null;
                    }
                }

                // 예약어 창 닫기
                if (_reservedWordsWindow != null)
                {
                    try
                    {
                        _reservedWordsWindow.Close();
                        _reservedWordsWindow = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing reserved words window");
                        _reservedWordsWindow = null;
                    }
                }

                // 시리얼 모니터 창 닫기
                if (_serialMonitorWindow != null)
                {
                    try
                    {
                        _serialMonitorWindow.Close();
                        _serialMonitorWindow = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing serial monitor window");
                        _serialMonitorWindow = null;
                    }
                }

                // API 모니터 창 닫기
                if (_apiMonitorWindow != null)
                {
                    try
                    {
                        _apiMonitorWindow.Close();
                        _apiMonitorWindow = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing API monitor window");
                        _apiMonitorWindow = null;
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during closing child windows");
            }
        }

        // IDisposable Implementation
        public void Dispose()
        {
            try
            {

                // 백그라운드 작업 취소
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                // 타이머 서비스 정지
                _timerService?.Stop();
                _timerService?.Dispose();

                // 시리얼 연결 해제 - 동기적으로 완료 대기
                if (_serialService?.IsConnected == true)
                {
                    try
                    {
                        var disconnectTask = _serialService.DisconnectAsync();
                        disconnectTask.Wait(TimeSpan.FromSeconds(5)); // 5초 타임아웃
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error disconnecting serial service during disposal");
                    }
                }

                // 시뮬레이션 정지
                if (_isSimulating)
                {
                    _serialDataSimulator?.Stop();
                }

                // 큐 매니저 정리 - 더 짧은 타임아웃으로 강제 종료
                try
                {
                    _queueManager?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disposing queue manager");
                }

                // 열린 창들 닫기
                CloseAllChildWindows();

                // CancellationTokenSource 정리
                _cancellationTokenSource.Dispose();

                // 강제 가비지 컬렉션
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
