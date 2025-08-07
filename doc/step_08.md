# Step 08: 오류 처리 및 로깅 시스템

## 개요
전체 애플리케이션에 걸친 포괄적인 오류 처리 및 로깅 시스템을 구현하여 안정성을 확보하고 문제 해결을 지원합니다.

## 상세 작업

### 8.1 로깅 시스템 아키텍처
- Microsoft.Extensions.Logging 기반 로깅 인프라
- 구조화된 로깅 (Structured Logging)
- 다중 로그 출력 (파일, 콘솔, UI, 원격)
- 로그 레벨별 필터링 및 라우팅

### 8.2 로깅 설정 및 구성
```xml
<!-- App.config 로깅 설정 -->
<logging>
  <logLevel>
    <add name="Default" minLevel="Information" />
    <add name="SimpleSerialToApi.Services.Serial" minLevel="Debug" />
    <add name="SimpleSerialToApi.Services.Api" minLevel="Warning" />
  </logLevel>
  <providers>
    <add name="File"
         enabled="true"
         path="logs/app_{Date}.log"
         maxFileSizeKB="10240"
         retainedFileCountLimit="30" />
    <add name="Console"
         enabled="true"
         includeScopes="true" />
    <add name="EventViewer"
         enabled="false"
         logName="Application"
         sourceName="SimpleSerialToApi" />
  </providers>
</logging>
```

### 8.3 커스텀 로거 및 확장
```csharp
public static class LoggerExtensions
{
    public static void LogSerialCommunication(this ILogger logger, string port, string action, string data = null)
    {
        logger.LogInformation("Serial {Action} on {Port}: {Data}", action, port, data);
    }
    
    public static void LogApiTransaction(this ILogger logger, string endpoint, string method, TimeSpan duration, bool success)
    {
        logger.LogInformation("API {Method} {Endpoint} completed in {Duration}ms - Success: {Success}", 
            method, endpoint, duration.TotalMilliseconds, success);
    }
    
    public static void LogQueueOperation(this ILogger logger, string operation, int messageCount, string queueName)
    {
        logger.LogDebug("Queue {Operation}: {MessageCount} messages in {QueueName}", 
            operation, messageCount, queueName);
    }
}
```

### 8.4 예외 처리 전략

#### 8.4.1 글로벌 예외 처리기
```csharp
public class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly INotificationService _notificationService;
    
    public void HandleUnhandledException(Exception exception, string context)
    {
        _logger.LogCritical(exception, "Unhandled exception in {Context}", context);
        
        // 사용자에게 친화적인 알림
        _notificationService.ShowError($"예기치 않은 오류가 발생했습니다: {context}");
        
        // 오류 보고서 생성
        GenerateErrorReport(exception, context);
    }
}
```

#### 8.4.2 도메인별 예외 클래스
```csharp
// Serial 통신 예외
public class SerialCommunicationException : Exception
{
    public string PortName { get; }
    public SerialErrorType ErrorType { get; }
    
    public SerialCommunicationException(string portName, SerialErrorType errorType, string message, Exception innerException = null) 
        : base(message, innerException)
    {
        PortName = portName;
        ErrorType = errorType;
    }
}

// API 통신 예외
public class ApiCommunicationException : Exception
{
    public string EndpointName { get; }
    public int? StatusCode { get; }
    public string ResponseContent { get; }
    
    public ApiCommunicationException(string endpointName, int? statusCode, string message, string responseContent = null) 
        : base(message)
    {
        EndpointName = endpointName;
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}

// 설정 관련 예외
public class ConfigurationException : Exception
{
    public string SectionName { get; }
    public string SettingName { get; }
    
    public ConfigurationException(string sectionName, string settingName, string message) : base(message)
    {
        SectionName = sectionName;
        SettingName = settingName;
    }
}
```

### 8.5 복구 전략 및 재시도 로직
```csharp
public interface IRecoveryStrategy<T>
{
    Task<RecoveryResult<T>> AttemptRecoveryAsync(Exception exception, RecoveryContext context);
    bool CanHandle(Exception exception);
    int MaxAttempts { get; }
}

public class SerialConnectionRecoveryStrategy : IRecoveryStrategy<bool>
{
    public async Task<RecoveryResult<bool>> AttemptRecoveryAsync(Exception exception, RecoveryContext context)
    {
        if (exception is SerialCommunicationException serialEx)
        {
            // Serial 포트 재연결 시도
            await Task.Delay(1000);
            // 복구 로직 구현
            return new RecoveryResult<bool> { Success = true, Result = true };
        }
        return new RecoveryResult<bool> { Success = false };
    }
}
```

### 8.6 애플리케이션 상태 모니터링
```csharp
public interface IHealthMonitor
{
    Task<HealthStatus> CheckApplicationHealthAsync();
    Task<ComponentHealthStatus> CheckComponentHealthAsync(string componentName);
    event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
}

public class ApplicationHealthMonitor : IHealthMonitor
{
    // Serial 연결, API 연결, Queue 상태 등을 종합적으로 모니터링
    private readonly IEnumerable<IHealthChecker> _healthCheckers;
}
```

### 8.7 오류 보고 및 진단
```csharp
public class DiagnosticReportGenerator
{
    public async Task<DiagnosticReport> GenerateReportAsync(Exception exception = null)
    {
        return new DiagnosticReport
        {
            Timestamp = DateTime.UtcNow,
            ApplicationVersion = GetApplicationVersion(),
            SystemInfo = GetSystemInformation(),
            ConfigurationSnapshot = await GetConfigurationSnapshotAsync(),
            RecentLogs = await GetRecentLogsAsync(TimeSpan.FromMinutes(10)),
            ExceptionDetails = exception != null ? GetExceptionDetails(exception) : null,
            PerformanceCounters = GetPerformanceCounters()
        };
    }
}
```

### 8.8 사용자 피드백 시스템
```csharp
public interface INotificationService
{
    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowError(string message, Exception exception = null);
    Task<bool> ShowConfirmationAsync(string message);
    void ShowProgress(string message, CancellationToken cancellationToken = default);
}
```

## 기술 요구사항
- Microsoft.Extensions.Logging
- Serilog (구조화된 로깅)
- Polly (재시도 및 회로 차단기)
- System.Diagnostics (성능 카운터)
- Windows Event Log

## 주요 로그 카테고리

### 8.9 로그 분류 및 형식
```csharp
public static class LogCategories
{
    public const string SerialCommunication = "SerialComm";
    public const string ApiCommunication = "ApiComm";
    public const string DataProcessing = "DataProc";
    public const string Configuration = "Config";
    public const string UserInterface = "UI";
    public const string Performance = "Perf";
    public const string Security = "Security";
}

// 구조화된 로그 형식 예제
logger.LogInformation("Message processed successfully: {MessageId} from {Source} to {Destination} in {Duration}ms", 
    messageId, source, destination, duration);
```

## 산출물
- [x] 로깅 인프라 및 설정
- [x] 커스텀 로거 확장 메서드들
- [x] 글로벌 예외 처리기
- [x] 도메인별 예외 클래스들
- [x] 복구 전략 인터페이스 및 구현체들
- [x] 애플리케이션 상태 모니터링 시스템
- [x] 진단 보고서 생성기
- [x] 사용자 알림 시스템
- [x] 로그 뷰어 UI 컴포넌트
- [x] 오류 처리 관련 단위 테스트

## 완료 조건
1. 모든 주요 작업이 적절한 로그 레벨로 기록됨
2. 예외 발생 시 자동 복구가 시도됨
3. 복구 불가능한 오류 시 사용자에게 명확한 안내 제공
4. 로그 파일이 크기 제한 및 보관 정책에 따라 관리됨
5. 구조화된 로그 검색 및 필터링이 가능함
6. 애플리케이션 상태가 실시간으로 모니터링됨
7. 진단 보고서가 문제 해결에 도움이 되는 정보 포함
8. UI를 통한 로그 뷰어가 정상 동작함

## 로그 레벨 가이드라인
- **Trace**: 상세한 실행 흐름 (개발 시에만)
- **Debug**: 디버깅 정보 (개발/테스트 환경)
- **Information**: 일반적인 애플리케이션 동작
- **Warning**: 예상치 못한 상황이지만 동작 가능
- **Error**: 오류 발생하였으나 애플리케이션 계속 실행
- **Critical**: 심각한 오류로 애플리케이션 종료 가능

## 다음 단계 의존성
이 단계가 완료되어야 Step 09 (테스트 프레임워크)를 진행할 수 있습니다.

## 예상 소요 시간
**3-4일 (24-32시간)**

## 주의사항
- 로그 성능 영향 최소화 (비동기 로깅)
- 민감 정보 로그 출력 금지
- 로그 파일 디스크 공간 관리
- 원격 로깅 시 보안 고려
- 로그 형식 일관성 유지

## 담당자 역할
- **개발자**: 로깅 시스템 구현, 예외 처리 로직 개발
- **운영 엔지니어**: 로그 수집 및 모니터링 시스템 설계
- **보안 담당자**: 로그 보안 및 민감정보 처리 검토
- **검토자**: 오류 처리 전략 및 복구 로직 검토