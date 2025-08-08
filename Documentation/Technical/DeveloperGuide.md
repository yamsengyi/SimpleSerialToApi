# SimpleSerialToApi 개발자 가이드

## 1. 개발 환경 설정

### 1.1 필수 도구
- **Visual Studio 2022** (Community 이상)
  - .NET 8 SDK
  - WPF 개발 도구
  - NuGet Package Manager
- **Git** 버전 관리
- **Windows SDK** (WPF 개발용)

### 1.2 선택적 도구
- **Visual Studio Code** (lightweight 개발)
- **JetBrains Rider**
- **LINQPad** (LINQ 쿼리 테스팅)
- **ILSpy** (IL 디컴파일러)
- **PerfView** (성능 분석)

### 1.3 개발 환경 검증
```bash
# .NET 버전 확인
dotnet --version  # 8.x.x 이상

# SDK 목록 확인
dotnet --list-sdks

# 프로젝트 빌드 테스트
dotnet build --configuration Debug
dotnet test
```

## 2. 프로젝트 구조

### 2.1 솔루션 구조
```
SimpleSerialToApi/
├── SimpleSerialToApi.sln              # 메인 솔루션 파일
├── SimpleSerialToApi/                 # 메인 WPF 애플리케이션
│   ├── App.xaml                       # 애플리케이션 진입점
│   ├── MainWindow.xaml                # 메인 윈도우 UI
│   ├── ViewModels/                    # MVVM 뷰 모델
│   ├── Views/                         # WPF 뷰
│   ├── Services/                      # 비즈니스 로직
│   ├── Models/                        # 데이터 모델
│   ├── Converters/                    # WPF 바인딩 컨버터
│   ├── Configuration/                 # 설정 관리
│   └── Interfaces/                    # 서비스 인터페이스
├── SimpleSerialToApi.Tests/           # 단위 테스트 프로젝트
└── Documentation/                     # 프로젝트 문서
```

### 2.2 네임스페이스 구조
```csharp
SimpleSerialToApi                      # 루트 네임스페이스
├── ViewModels                         # MVVM 뷰모델
├── Views                              # WPF 뷰
├── Services                           # 핵심 서비스
│   ├── Serial                         # Serial 통신 관련
│   ├── Api                            # API 통신 관련
│   ├── Queue                          # 메시지 큐 관련
│   └── Configuration                  # 설정 관리
├── Models                             # 데이터 모델
│   ├── Configuration                  # 설정 모델
│   ├── Serial                         # Serial 데이터 모델
│   └── Api                            # API 데이터 모델
├── Interfaces                         # 서비스 인터페이스
├── Converters                         # WPF 컨버터
└── Configuration                      # 설정 관리자
```

## 3. 핵심 아키텍처 패턴

### 3.1 MVVM Pattern
```csharp
// ViewModel 기본 구조
public class MainViewModel : ViewModelBase
{
    private readonly ISerialCommunicationService _serialService;
    private readonly IApiClientService _apiService;
    
    public MainViewModel(
        ISerialCommunicationService serialService,
        IApiClientService apiService)
    {
        _serialService = serialService;
        _apiService = apiService;
        
        // Commands 초기화
        ConnectCommand = new RelayCommand(ExecuteConnect, CanExecuteConnect);
        DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect);
    }
    
    // Properties
    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public bool IsConnected => _serialService.IsConnected;
    
    // Commands
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
}
```

### 3.2 Dependency Injection
```csharp
// App.xaml.cs - DI 컨테이너 설정
public partial class App : Application
{
    private IServiceProvider _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Views & ViewModels
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();
        
        // Services
        services.AddSingleton<ISerialCommunicationService, SerialCommunicationService>();
        services.AddSingleton<IApiClientService, ApiClientService>();
        services.AddSingleton<IMessageQueueService, MessageQueueService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Configuration
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        
        // Logging
        services.AddLogging(builder => builder.AddSerilog());
    }
}
```

### 3.3 Repository Pattern
```csharp
// 설정 데이터 액세스
public interface IConfigurationRepository
{
    Task<SerialConfiguration> GetSerialConfigAsync();
    Task SaveSerialConfigAsync(SerialConfiguration config);
    Task<ApiConfiguration> GetApiConfigAsync();
    Task SaveApiConfigAsync(ApiConfiguration config);
}

public class AppConfigRepository : IConfigurationRepository
{
    private readonly IConfiguration _configuration;
    
    public AppConfigRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<SerialConfiguration> GetSerialConfigAsync()
    {
        return new SerialConfiguration
        {
            PortName = _configuration["Serial:PortName"] ?? "COM1",
            BaudRate = int.Parse(_configuration["Serial:BaudRate"] ?? "9600"),
            DataBits = int.Parse(_configuration["Serial:DataBits"] ?? "8"),
            Parity = Enum.Parse<Parity>(_configuration["Serial:Parity"] ?? "None"),
            StopBits = Enum.Parse<StopBits>(_configuration["Serial:StopBits"] ?? "One")
        };
    }
}
```

## 4. 새로운 기능 개발 가이드

### 4.1 새로운 Serial 장비 파서 추가

#### Step 1: 파서 인터페이스 구현
```csharp
public class NewDeviceParser : IDataParser
{
    public string DeviceType => "NewDevice";
    
    public bool CanParse(byte[] data)
    {
        // 데이터가 이 장비 타입인지 판단
        var text = Encoding.UTF8.GetString(data);
        return text.StartsWith("NEWDEV:");
    }
    
    public ParseResult Parse(byte[] data)
    {
        try
        {
            var text = Encoding.UTF8.GetString(data);
            
            // 파싱 로직 구현
            // 예: "NEWDEV:TEMP=25.5,HUM=60.0"
            var match = Regex.Match(text, @"NEWDEV:TEMP=([+-]?\d+\.?\d*),HUM=([+-]?\d+\.?\d*)");
            
            if (match.Success)
            {
                return new ParseResult
                {
                    Success = true,
                    DeviceId = "NEWDEV_001",
                    Timestamp = DateTime.UtcNow,
                    Data = new Dictionary<string, object>
                    {
                        ["temperature"] = double.Parse(match.Groups[1].Value),
                        ["humidity"] = double.Parse(match.Groups[2].Value)
                    }
                };
            }
            
            return ParseResult.Failed("Invalid data format");
        }
        catch (Exception ex)
        {
            return ParseResult.Failed($"Parsing error: {ex.Message}");
        }
    }
}
```

#### Step 2: DI 컨테이너에 등록
```csharp
// App.xaml.cs ConfigureServices 메소드에 추가
services.AddTransient<IDataParser, NewDeviceParser>();
```

#### Step 3: 단위 테스트 작성
```csharp
[TestFixture]
public class NewDeviceParserTests
{
    private NewDeviceParser _parser;
    
    [SetUp]
    public void SetUp()
    {
        _parser = new NewDeviceParser();
    }
    
    [Test]
    public void Parse_ValidData_ReturnsSuccess()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("NEWDEV:TEMP=25.5,HUM=60.0");
        
        // Act
        var result = _parser.Parse(data);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data["temperature"], Is.EqualTo(25.5));
        Assert.That(result.Data["humidity"], Is.EqualTo(60.0));
    }
    
    [Test]
    public void CanParse_ValidData_ReturnsTrue()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("NEWDEV:TEMP=25.5,HUM=60.0");
        
        // Act
        var result = _parser.CanParse(data);
        
        // Assert
        Assert.That(result, Is.True);
    }
}
```

### 4.2 새로운 인증 방식 추가

#### Step 1: 인증 핸들러 구현
```csharp
public class CustomAuthHandler : IAuthenticationHandler
{
    public string AuthType => "Custom";
    
    public async Task<AuthenticationResult> AuthenticateAsync(HttpRequestMessage request, AuthenticationConfig config)
    {
        try
        {
            // 커스텀 인증 로직
            var signature = GenerateSignature(request, config.SecretKey);
            request.Headers.Add("X-Custom-Signature", signature);
            request.Headers.Add("X-Custom-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failed($"Authentication failed: {ex.Message}");
        }
    }
    
    private string GenerateSignature(HttpRequestMessage request, string secretKey)
    {
        // HMAC-SHA256 시그니처 생성 로직
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var method = request.Method.Method;
        var uri = request.RequestUri?.ToString();
        
        var message = $"{method}|{uri}|{timestamp}";
        
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
```

#### Step 2: 설정 모델 확장
```csharp
public class AuthenticationConfig
{
    public string Type { get; set; }
    public string Token { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string SecretKey { get; set; } // 새로운 커스텀 인증용
}
```

### 4.3 새로운 UI 컨트롤 추가

#### Step 1: UserControl 생성
```xml
<!-- DataVisualizationControl.xaml -->
<UserControl x:Class="SimpleSerialToApi.Views.DataVisualizationControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <TextBlock Text="Real-time Data Visualization" 
                   FontSize="16" FontWeight="Bold" 
                   Grid.Row="0" Margin="10"/>
        
        <Canvas x:Name="ChartCanvas" 
                Grid.Row="1" 
                Background="White"
                Margin="10"/>
    </Grid>
</UserControl>
```

#### Step 2: Code-behind 구현
```csharp
public partial class DataVisualizationControl : UserControl
{
    public DataVisualizationControl()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetService<DataVisualizationViewModel>();
    }
    
    public static readonly DependencyProperty DataSourceProperty = 
        DependencyProperty.Register(
            nameof(DataSource), 
            typeof(ObservableCollection<DataPoint>), 
            typeof(DataVisualizationControl),
            new PropertyMetadata(null, OnDataSourceChanged));
    
    public ObservableCollection<DataPoint> DataSource
    {
        get => (ObservableCollection<DataPoint>)GetValue(DataSourceProperty);
        set => SetValue(DataSourceProperty, value);
    }
    
    private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataVisualizationControl control)
        {
            control.UpdateChart();
        }
    }
    
    private void UpdateChart()
    {
        // 차트 업데이트 로직
        ChartCanvas.Children.Clear();
        
        if (DataSource == null || DataSource.Count == 0)
            return;
            
        // 데이터 포인트를 기반으로 차트 렌더링
        DrawChart();
    }
}
```

## 5. 빌드 및 배포

### 5.1 개발 빌드
```bash
# Debug 빌드
dotnet build --configuration Debug

# Release 빌드
dotnet build --configuration Release

# 특정 프로젝트만 빌드
dotnet build SimpleSerialToApi/SimpleSerialToApi.csproj --configuration Release
```

### 5.2 테스트 실행
```bash
# 모든 테스트 실행
dotnet test

# 특정 테스트 프로젝트 실행
dotnet test SimpleSerialToApi.Tests/

# 코드 커버리지 포함
dotnet test --collect:"XPlat Code Coverage"

# 특정 테스트 클래스 실행
dotnet test --filter "ClassName=SerialCommunicationServiceTests"
```

### 5.3 패키징 및 배포
```bash
# Self-contained 배포 패키지 생성
dotnet publish SimpleSerialToApi/SimpleSerialToApi.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish

# Framework-dependent 배포 패키지 생성  
dotnet publish SimpleSerialToApi/SimpleSerialToApi.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained false \
  --output ./publish
```

## 6. 디버깅 가이드

### 6.1 Visual Studio 디버깅 설정
```json
// launch.json (VS Code 사용시)
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Debug SimpleSerialToApi",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/SimpleSerialToApi/bin/Debug/net8.0-windows/SimpleSerialToApi.exe",
            "args": [],
            "cwd": "${workspaceFolder}/SimpleSerialToApi",
            "console": "internalConsole",
            "stopAtEntry": false
        }
    ]
}
```

### 6.2 Serial 통신 디버깅
```csharp
// Serial 데이터 로깅을 위한 디버그 도우미
public class SerialDebugHelper
{
    private static readonly ILogger _logger = Log.ForContext<SerialDebugHelper>();
    
    public static void LogRawData(byte[] data, string direction)
    {
        var hex = BitConverter.ToString(data).Replace("-", " ");
        var ascii = Encoding.UTF8.GetString(data, 0, data.Length);
        
        _logger.Debug("Serial {Direction}: Hex=[{Hex}] ASCII=[{Ascii}]", 
            direction, hex, ascii);
    }
    
    public static void LogPortStatus(SerialPort port)
    {
        _logger.Debug("Port Status: Name={PortName} IsOpen={IsOpen} BaudRate={BaudRate}", 
            port.PortName, port.IsOpen, port.BaudRate);
    }
}
```

### 6.3 성능 프로파일링
```csharp
// 성능 측정을 위한 간단한 프로파일러
public class SimpleProfiler : IDisposable
{
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private static readonly ILogger _logger = Log.ForContext<SimpleProfiler>();
    
    public SimpleProfiler(string operationName)
    {
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.Information("Operation {Operation} took {Duration}ms", 
            _operationName, _stopwatch.ElapsedMilliseconds);
    }
}

// 사용 예제
using (new SimpleProfiler("DataParsing"))
{
    var result = parser.Parse(data);
}
```

## 7. 코딩 표준 및 가이드라인

### 7.1 C# 코딩 표준
- **PascalCase**: 클래스, 메소드, 프로퍼티명
- **camelCase**: 지역변수, 필드명 (private 필드는 _camelCase)
- **UPPER_CASE**: 상수명
- **async 메소드**: Async 접미사 사용
- **인터페이스**: I 접두사 사용

```csharp
// 좋은 예
public class SerialCommunicationService : ISerialCommunicationService
{
    private readonly ILogger<SerialCommunicationService> _logger;
    private const int DEFAULT_TIMEOUT = 5000;
    
    public async Task<bool> ConnectAsync(string portName)
    {
        // 구현
    }
    
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // 구현
    }
}
```

### 7.2 XAML 가이드라인
```xml
<!-- 좋은 예: 적절한 인덴트와 속성 정렬 -->
<Button Name="ConnectButton"
        Content="Connect"
        Command="{Binding ConnectCommand}"
        IsEnabled="{Binding CanConnect}"
        Style="{StaticResource PrimaryButtonStyle}"
        Margin="10,5"
        MinWidth="100"
        Height="35" />
```

### 7.3 에러 핸들링 가이드
```csharp
public class SerialService
{
    public async Task<Result<string>> ReadDataAsync()
    {
        try
        {
            // 작업 수행
            var data = await ReadFromPortAsync();
            return Result<string>.Success(data);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Serial read timeout");
            return Result<string>.Failed("Read timeout", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Serial IO error");
            return Result<string>.Failed("IO error", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during serial read");
            return Result<string>.Failed("Unexpected error", ex);
        }
    }
}

// Result 타입 정의
public class Result<T>
{
    public bool Success { get; }
    public T Value { get; }
    public string Error { get; }
    public Exception Exception { get; }
    
    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failed(string error, Exception ex = null) => new(false, default, error, ex);
}
```

## 8. 확장 포인트

### 8.1 플러그인 아키텍처
```csharp
public interface IPlugin
{
    string Name { get; }
    Version Version { get; }
    void Initialize(IServiceProvider serviceProvider);
    void Shutdown();
}

public interface IDataParserPlugin : IPlugin
{
    IDataParser CreateParser();
}

// 플러그인 로더
public class PluginLoader
{
    public IEnumerable<T> LoadPlugins<T>(string pluginDirectory) where T : IPlugin
    {
        var plugins = new List<T>();
        
        foreach (var file in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            var assembly = Assembly.LoadFrom(file);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface);
                
            foreach (var type in pluginTypes)
            {
                var plugin = (T)Activator.CreateInstance(type);
                plugins.Add(plugin);
            }
        }
        
        return plugins;
    }
}
```

### 8.2 이벤트 기반 확장
```csharp
public static class ApplicationEvents
{
    public static event EventHandler<DataReceivedEventArgs> DataReceived;
    public static event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
    public static event EventHandler<ErrorEventArgs> ErrorOccurred;
    
    public static void OnDataReceived(ParsedData data)
    {
        DataReceived?.Invoke(null, new DataReceivedEventArgs(data));
    }
}

// 확장 모듈에서 이벤트 구독
public class CustomDataProcessor
{
    public void Initialize()
    {
        ApplicationEvents.DataReceived += OnDataReceived;
    }
    
    private void OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        // 커스텀 데이터 처리 로직
    }
}
```

## 9. 테스팅 전략

### 9.1 단위 테스트
```csharp
[TestFixture]
public class SerialCommunicationServiceTests
{
    private Mock<ISerialPort> _mockSerialPort;
    private Mock<ILogger<SerialCommunicationService>> _mockLogger;
    private SerialCommunicationService _service;
    
    [SetUp]
    public void SetUp()
    {
        _mockSerialPort = new Mock<ISerialPort>();
        _mockLogger = new Mock<ILogger<SerialCommunicationService>>();
        _service = new SerialCommunicationService(_mockSerialPort.Object, _mockLogger.Object);
    }
    
    [Test]
    public async Task ConnectAsync_ValidPort_ReturnsTrue()
    {
        // Arrange
        _mockSerialPort.Setup(x => x.Open()).Returns(Task.CompletedTask);
        _mockSerialPort.Setup(x => x.IsOpen).Returns(true);
        
        // Act
        var result = await _service.ConnectAsync("COM1");
        
        // Assert
        Assert.That(result, Is.True);
        _mockSerialPort.Verify(x => x.Open(), Times.Once);
    }
}
```

### 9.2 통합 테스트
```csharp
[TestFixture]
public class SerialToApiIntegrationTests
{
    private TestServer _testServer;
    private HttpClient _testClient;
    private SerialPortEmulator _serialEmulator;
    
    [SetUp]
    public void SetUp()
    {
        // Mock API 서버 설정
        _testServer = new TestServer(new WebHostBuilder()
            .UseStartup<TestApiStartup>());
        _testClient = _testServer.CreateClient();
        
        // Serial 포트 에뮬레이터 설정
        _serialEmulator = new SerialPortEmulator("COM99");
    }
    
    [Test]
    public async Task EndToEnd_SerialDataToApi_Success()
    {
        // Arrange
        var testData = "TEMP:25.5,HUM:60.0\r\n";
        
        // Act
        _serialEmulator.SendData(testData);
        await Task.Delay(1000); // 처리 대기
        
        // Assert
        var requests = _testServer.GetReceivedRequests();
        Assert.That(requests.Count, Is.EqualTo(1));
        
        var request = requests.First();
        var body = await request.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<SensorData>(body);
        
        Assert.That(data.Temperature, Is.EqualTo(25.5));
        Assert.That(data.Humidity, Is.EqualTo(60.0));
    }
}
```

## 10. 배포 자동화

### 10.1 GitHub Actions 워크플로우
```yaml
# .github/workflows/build-and-test.yml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --logger trx
      
    - name: Publish
      run: dotnet publish SimpleSerialToApi/SimpleSerialToApi.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish
      
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: SimpleSerialToApi
        path: ./publish
```

### 10.2 MSI 패키지 생성 스크립트
```powershell
# build-installer.ps1
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

Write-Host "Building SimpleSerialToApi v$Version..." -ForegroundColor Green

# 1. Clean and build
dotnet clean --configuration $Configuration
dotnet build --configuration $Configuration --no-restore

# 2. Run tests
$testResult = dotnet test --configuration $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed!"
    exit 1
}

# 3. Publish application
dotnet publish SimpleSerialToApi/SimpleSerialToApi.csproj `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output "./publish"

# 4. Build MSI installer (WiX 필요)
& "C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe" `
    -dVersion=$Version `
    -dConfiguration=$Configuration `
    Installer/SimpleSerialToApi.wxs

& "C:\Program Files (x86)\WiX Toolset v3.11\bin\light.exe" `
    -out "SimpleSerialToApi-$Version.msi" `
    SimpleSerialToApi.wixobj

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Installer created: SimpleSerialToApi-$Version.msi" -ForegroundColor Yellow
```

## 11. 문제 해결 및 FAQ

### 11.1 일반적인 개발 문제

#### Serial Port Access Denied
```csharp
// 해결책: 관리자 권한으로 Visual Studio 실행
// 또는 개발용 Serial Port Emulator 사용

// com0com 설치 후 가상 포트 쌍 생성
// COM98 <-> COM99 연결하여 테스트
```

#### WPF Data Binding 문제
```csharp
// 디버깅: Output 창에서 Binding 오류 확인
// 또는 PresentationTraceSources.DataBindingSource 사용

public MainWindow()
{
    InitializeComponent();
    
#if DEBUG
    // Binding 오류를 콘솔에 출력
    PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
    PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
#endif
}
```

### 11.2 성능 최적화 팁

#### 메모리 사용량 최적화
```csharp
// ObservableCollection 대신 필요시에만 업데이트
public class ThrottledObservableCollection<T> : ObservableCollection<T>
{
    private readonly Timer _updateTimer;
    private bool _needsUpdate;
    
    public ThrottledObservableCollection(int throttleMs = 100)
    {
        _updateTimer = new Timer(throttleMs);
        _updateTimer.Elapsed += OnUpdateTimer;
    }
    
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        _needsUpdate = true;
        _updateTimer.Stop();
        _updateTimer.Start();
    }
    
    private void OnUpdateTimer(object sender, ElapsedEventArgs e)
    {
        if (_needsUpdate)
        {
            Application.Current.Dispatcher.Invoke(() => 
                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
            _needsUpdate = false;
        }
        _updateTimer.Stop();
    }
}
```

이 개발자 가이드를 참고하여 SimpleSerialToApi 프로젝트를 효율적으로 확장하고 유지보수할 수 있습니다.