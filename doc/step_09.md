# Step 09: 테스트 프레임워크 및 단위 테스트

## 개요
전체 애플리케이션에 대한 포괄적인 테스트 프레임워크를 구축하고, 단위 테스트, 통합 테스트, 및 UI 테스트를 구현하여 코드 품질과 안정성을 확보합니다.

## 상세 작업

### 9.1 테스트 프로젝트 구조
```
SimpleSerialToApi.Tests/
├── Unit/                           # 단위 테스트
│   ├── Services/
│   │   ├── SerialCommunicationServiceTests.cs
│   │   ├── ApiClientServiceTests.cs
│   │   ├── ConfigurationServiceTests.cs
│   │   ├── DataParsingServiceTests.cs
│   │   └── MessageQueueServiceTests.cs
│   ├── Models/
│   └── Utils/
├── Integration/                    # 통합 테스트
│   ├── SerialToApiIntegrationTests.cs
│   ├── ConfigurationIntegrationTests.cs
│   └── EndToEndWorkflowTests.cs
├── UI/                            # UI 테스트
│   ├── ViewModels/
│   │   ├── MainViewModelTests.cs
│   │   └── SettingsViewModelTests.cs
│   └── Views/
├── Mocks/                         # Mock 클래스들
│   ├── MockSerialPort.cs
│   ├── MockHttpMessageHandler.cs
│   └── MockConfigurationService.cs
└── TestData/                      # 테스트 데이터
    ├── SampleSerialData/
    ├── SampleApiResponses/
    └── TestConfigurations/
```

### 9.2 테스트 인프라 구성
```csharp
// 테스트 베이스 클래스
public abstract class TestBase : IDisposable
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;
    protected readonly TestContext TestContext;
    
    protected TestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();
        TestContext = new TestContext();
    }
    
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // 테스트용 서비스 등록
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(CreateTestConfiguration());
    }
}
```

### 9.3 Serial 통신 서비스 단위 테스트
```csharp
[TestClass]
public class SerialCommunicationServiceTests : TestBase
{
    private SerialCommunicationService _service;
    private Mock<ISerialPort> _mockSerialPort;
    private Mock<ILogger<SerialCommunicationService>> _mockLogger;
    
    [TestInitialize]
    public void Setup()
    {
        _mockSerialPort = new Mock<ISerialPort>();
        _mockLogger = new Mock<ILogger<SerialCommunicationService>>();
        _service = new SerialCommunicationService(_mockSerialPort.Object, _mockLogger.Object);
    }
    
    [TestMethod]
    public async Task ConnectAsync_WithValidPort_ShouldReturnTrue()
    {
        // Arrange
        _mockSerialPort.Setup(p => p.IsOpen).Returns(false);
        _mockSerialPort.Setup(p => p.Open()).Verifiable();
        
        // Act
        var result = await _service.ConnectAsync();
        
        // Assert
        Assert.IsTrue(result);
        _mockSerialPort.Verify(p => p.Open(), Times.Once);
    }
    
    [TestMethod]
    public async Task SendDataAsync_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        _mockSerialPort.Setup(p => p.IsOpen).Returns(true);
        _mockSerialPort.Setup(p => p.Write(testData, 0, testData.Length)).Verifiable();
        
        // Act
        var result = await _service.SendDataAsync(testData);
        
        // Assert
        Assert.IsTrue(result);
        _mockSerialPort.Verify(p => p.Write(testData, 0, testData.Length), Times.Once);
    }
}
```

### 9.4 API 클라이언트 서비스 단위 테스트
```csharp
[TestClass]
public class ApiClientServiceTests : TestBase
{
    private ApiClientService _service;
    private Mock<HttpMessageHandler> _mockHandler;
    private HttpClient _httpClient;
    
    [TestInitialize]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _service = new ApiClientService(_httpClient, Mock.Of<ILogger<ApiClientService>>());
    }
    
    [TestMethod]
    public async Task PostAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var testData = new { Temperature = 25.5, Humidity = 60.0 };
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\": true}", Encoding.UTF8, "application/json")
        };
        
        _mockHandler.Setup(h => h.SendAsync(
            It.IsAny<HttpRequestMessage>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        
        // Act
        var result = await _service.PostAsync("TestEndpoint", testData);
        
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(200, result.StatusCode);
    }
}
```

### 9.5 데이터 파싱 서비스 단위 테스트
```csharp
[TestClass]
public class DataParsingServiceTests : TestBase
{
    private DataParsingService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _service = new DataParsingService(Mock.Of<ILogger<DataParsingService>>());
    }
    
    [TestMethod]
    public void Parse_WithTemperatureData_ShouldReturnParsedData()
    {
        // Arrange
        var rawData = new RawSerialData
        {
            Data = Encoding.UTF8.GetBytes("TEMP:25.5C;HUMID:60.0%"),
            DataFormat = "TEXT",
            ReceivedTime = DateTime.UtcNow
        };
        
        var parsingRule = new ParsingRule
        {
            Name = "TemperatureSensor",
            Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%",
            Fields = new[] { "temperature", "humidity" }
        };
        
        // Act
        var result = _service.Parse(rawData, parsingRule);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(25.5m, result.Fields["temperature"]);
        Assert.AreEqual(60.0m, result.Fields["humidity"]);
    }
}
```

### 9.6 Message Queue 단위 테스트
```csharp
[TestClass]
public class MessageQueueTests : TestBase
{
    private ConcurrentMessageQueue<string> _queue;
    
    [TestInitialize]
    public void Setup()
    {
        var config = new QueueConfiguration { MaxSize = 100 };
        _queue = new ConcurrentMessageQueue<string>(config);
    }
    
    [TestMethod]
    public async Task EnqueueAsync_WithValidMessage_ShouldReturnTrue()
    {
        // Arrange
        var message = new QueueMessage<string> { Payload = "Test Message" };
        
        // Act
        var result = await _queue.EnqueueAsync(message);
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, _queue.Count);
    }
    
    [TestMethod]
    public async Task DequeueAsync_WithMessages_ShouldReturnMessage()
    {
        // Arrange
        var originalMessage = new QueueMessage<string> { Payload = "Test Message" };
        await _queue.EnqueueAsync(originalMessage);
        
        // Act
        var dequeuedMessage = await _queue.DequeueAsync();
        
        // Assert
        Assert.IsNotNull(dequeuedMessage);
        Assert.AreEqual("Test Message", dequeuedMessage.Payload);
        Assert.AreEqual(0, _queue.Count);
    }
}
```

### 9.7 통합 테스트
```csharp
[TestClass]
public class EndToEndWorkflowTests : TestBase
{
    private IServiceProvider _serviceProvider;
    
    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // 실제 서비스들을 등록하되, 외부 의존성은 Mock으로 대체
        services.AddSingleton<ISerialCommunicationService, SerialCommunicationService>();
        services.AddSingleton<IApiClientService, ApiClientService>();
        services.AddSingleton<IDataParsingService, DataParsingService>();
        services.AddSingleton<IMessageQueue<MappedApiData>, ConcurrentMessageQueue<MappedApiData>>();
        
        // Mock 외부 의존성
        services.AddSingleton(CreateMockSerialPort());
        services.AddSingleton(CreateMockHttpClient());
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [TestMethod]
    public async Task SerialDataToApiWorkflow_ShouldProcessSuccessfully()
    {
        // Arrange
        var serialService = _serviceProvider.GetRequiredService<ISerialCommunicationService>();
        var parsingService = _serviceProvider.GetRequiredService<IDataParsingService>();
        var apiService = _serviceProvider.GetRequiredService<IApiClientService>();
        
        // Act & Assert
        // 1. Serial 연결
        var connected = await serialService.ConnectAsync();
        Assert.IsTrue(connected);
        
        // 2. 데이터 수신 시뮬레이션
        var rawData = new RawSerialData { Data = Encoding.UTF8.GetBytes("TEMP:25.5C") };
        
        // 3. 데이터 파싱
        var parsedData = parsingService.Parse(rawData, GetTestParsingRule());
        Assert.IsNotNull(parsedData);
        
        // 4. API 전송
        var apiResponse = await apiService.PostAsync("TestEndpoint", parsedData);
        Assert.IsTrue(apiResponse.IsSuccess);
    }
}
```

### 9.8 UI 테스트 (ViewModel)
```csharp
[TestClass]
public class MainViewModelTests : TestBase
{
    private MainViewModel _viewModel;
    
    [TestInitialize]
    public void Setup()
    {
        var mockSerialService = new Mock<ISerialCommunicationService>();
        var mockApiService = new Mock<IApiClientService>();
        _viewModel = new MainViewModel(mockSerialService.Object, mockApiService.Object);
    }
    
    [TestMethod]
    public void StartApplicationCommand_WhenExecuted_ShouldChangeStatus()
    {
        // Arrange
        Assert.IsFalse(_viewModel.IsApplicationRunning);
        
        // Act
        _viewModel.StartApplicationCommand.Execute(null);
        
        // Assert
        Assert.IsTrue(_viewModel.IsApplicationRunning);
        Assert.AreEqual("Running", _viewModel.ApplicationStatus);
    }
}
```

### 9.9 성능 테스트
```csharp
[TestClass]
public class PerformanceTests : TestBase
{
    [TestMethod]
    public async Task DataParsing_With1000Messages_ShouldCompleteInTime()
    {
        // Arrange
        var service = new DataParsingService(Mock.Of<ILogger<DataParsingService>>());
        var messages = GenerateTestMessages(1000);
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        foreach (var message in messages)
        {
            var parsed = service.Parse(message, GetTestParsingRule());
        }
        
        stopwatch.Stop();
        
        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Parsing took too long: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

### 9.10 테스트 데이터 및 유틸리티
```csharp
public static class TestDataGenerator
{
    public static RawSerialData GenerateTemperatureData(decimal temperature, decimal humidity)
    {
        return new RawSerialData
        {
            Data = Encoding.UTF8.GetBytes($"TEMP:{temperature}C;HUMID:{humidity}%"),
            DataFormat = "TEXT",
            ReceivedTime = DateTime.UtcNow,
            DeviceId = "TEST_DEVICE_01"
        };
    }
    
    public static List<RawSerialData> GenerateTestMessages(int count)
    {
        var messages = new List<RawSerialData>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            messages.Add(GenerateTemperatureData(
                (decimal)(random.NextDouble() * 50),
                (decimal)(random.NextDouble() * 100)
            ));
        }
        
        return messages;
    }
}
```

## 필요한 NuGet 패키지
- **MSTest.TestFramework**: Microsoft 테스트 프레임워크
- **Moq**: Mocking 프레임워크
- **FluentAssertions**: 가독성 좋은 어설션
- **Microsoft.Extensions.DependencyInjection**: DI 컨테이너
- **Coverlet.collector**: 코드 커버리지 수집
- **ReportGenerator**: 커버리지 보고서 생성

## 산출물
- [x] 단위 테스트 프로젝트 구조
- [x] 모든 서비스 클래스의 단위 테스트
- [x] Mock 클래스들 및 테스트 유틸리티
- [x] 통합 테스트 시나리오
- [x] ViewModel 단위 테스트
- [x] 성능 테스트 케이스
- [x] 테스트 데이터 생성기
- [x] 테스트 실행 스크립트
- [x] 코드 커버리지 보고서

## 완료 조건
1. 모든 주요 서비스 클래스의 단위 테스트가 작성됨
2. 테스트 커버리지가 80% 이상 달성됨
3. 모든 테스트가 CI/CD 파이프라인에서 자동 실행됨
4. 통합 테스트가 전체 워크플로우를 검증함
5. 성능 테스트가 요구사항을 만족함
6. Mock을 활용하여 외부 의존성이 격리됨
7. 테스트 실행 시간이 5분 이내임
8. 테스트 결과가 명확하게 보고됨

## 테스트 전략
- **단위 테스트**: 각 클래스/메서드의 개별 기능 검증
- **통합 테스트**: 컴포넌트 간 상호작용 검증
- **UI 테스트**: ViewModel 및 바인딩 로직 검증
- **성능 테스트**: 처리량 및 응답시간 검증
- **회귀 테스트**: 기존 기능 보호

## 다음 단계 의존성
이 단계가 완료되어야 Step 10 (문서화 및 배포)를 진행할 수 있습니다.

## 예상 소요 시간
**4-5일 (32-40시간)**

## 주의사항
- 외부 의존성 (Serial 포트, HTTP) 격리
- 테스트 간 상태 공유 방지
- 테스트 데이터 정리 (Cleanup)
- 비동기 테스트 시 타이밍 이슈 주의
- CI 환경에서의 테스트 안정성

## 담당자 역할
- **개발자**: 단위 테스트 작성, Mock 클래스 구현
- **QA 엔지니어**: 통합 테스트 시나리오 설계, 테스트 검증
- **DevOps 엔지니어**: CI/CD 파이프라인 테스트 통합
- **검토자**: 테스트 전략 및 커버리지 검토