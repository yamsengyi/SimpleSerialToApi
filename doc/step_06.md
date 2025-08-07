# Step 06: API 연동 계층 구현

## 개요
Message Queue에서 처리된 데이터를 실제 REST API로 전송하는 계층을 구현합니다. 다양한 인증 방식, 오류 처리, 및 응답 관리 기능을 포함합니다.

## 상세 작업

### 6.1 API 클라이언트 서비스 설계
- `IApiClientService` 인터페이스 정의
- `HttpApiClientService` 구현 클래스
- API 엔드포인트별 클라이언트 관리
- Connection pooling 및 재사용

### 6.2 인증 시스템
```csharp
public interface IApiAuthenticator
{
    Task<AuthenticationResult> AuthenticateAsync(ApiEndpointConfig endpoint);
    Task<bool> RefreshTokenAsync(ApiEndpointConfig endpoint);
    void ClearAuthentication(string endpointName);
}

// 지원 인증 방식
public enum AuthenticationType
{
    None,
    BasicAuth,
    BearerToken,
    ApiKey,
    OAuth2,
    Custom
}
```

### 6.3 API 전송 관리자
```csharp
public interface IApiTransmissionManager
{
    Task<ApiResponse> SendDataAsync(MappedApiData data);
    Task<BatchApiResponse> SendBatchAsync(List<MappedApiData> dataList);
    Task<ApiResponse> RetryFailedRequestAsync(FailedApiRequest request);
    Task<HealthCheckResult> CheckEndpointHealthAsync(string endpointName);
}
```

### 6.4 응답 처리 및 오류 관리
```csharp
public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string ResponseContent { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime Timestamp { get; set; }
    public string ErrorMessage { get; set; }
    public string MessageId { get; set; }
}

public class FailedApiRequest
{
    public string MessageId { get; set; }
    public MappedApiData OriginalData { get; set; }
    public string EndpointName { get; set; }
    public int AttemptCount { get; set; }
    public DateTime LastAttemptTime { get; set; }
    public string LastErrorMessage { get; set; }
    public RetryPolicy RetryPolicy { get; set; }
}
```

### 6.5 HTTP 클라이언트 설정
```csharp
public class HttpClientConfiguration
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;
    public bool EnableCompression { get; set; } = true;
    public Dictionary<string, string> DefaultHeaders { get; set; }
    public ProxyConfiguration Proxy { get; set; }
    public SslConfiguration Ssl { get; set; }
}
```

### 6.6 App.Config API 설정 확장
```xml
<apiConfiguration>
  <endpoints>
    <add name="SensorDataEndpoint"
         url="https://api.example.com/sensor-data"
         method="POST"
         authType="Bearer"
         authToken="encrypted_token_here"
         timeout="30000"
         retryAttempts="3"
         contentType="application/json" />
  </endpoints>
  <httpClient>
    <add key="DefaultTimeout" value="30000" />
    <add key="MaxConcurrentRequests" value="10" />
    <add key="EnableCompression" value="true" />
  </httpClient>
  <retry>
    <add key="DefaultRetryAttempts" value="3" />
    <add key="RetryDelayMs" value="1000" />
    <add key="UseExponentialBackoff" value="true" />
  </retry>
</apiConfiguration>
```

### 6.7 API 전송 통계 및 모니터링
- 전송 성공/실패 통계
- 응답 시간 측정
- API 엔드포인트별 상태 모니터링
- 처리량 및 성능 메트릭

## 기술 요구사항
- System.Net.Http.HttpClient 활용
- Polly 라이브러리 (재시도 정책)
- JSON 직렬화 (Newtonsoft.Json)
- 비동기 HTTP 통신
- Connection pooling 최적화

## 주요 클래스 및 인터페이스

### IApiClientService
```csharp
public interface IApiClientService
{
    Task<ApiResponse> PostAsync<T>(string endpointName, T data);
    Task<ApiResponse> PutAsync<T>(string endpointName, T data);
    Task<ApiResponse> GetAsync(string endpointName, Dictionary<string, string> queryParams = null);
    Task<BatchApiResponse> PostBatchAsync<T>(string endpointName, List<T> dataList);
    Task<HealthCheckResult> HealthCheckAsync(string endpointName);
}
```

### IRetryPolicy
```csharp
public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
    bool ShouldRetry(Exception exception, int attemptNumber);
    TimeSpan GetDelay(int attemptNumber);
    int MaxAttempts { get; }
}
```

### ApiStatistics
```csharp
public class ApiStatistics
{
    public string EndpointName { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime LastSuccessfulRequest { get; set; }
    public DateTime LastFailedRequest { get; set; }
}
```

## 산출물
- [x] `IApiClientService` 인터페이스 및 구현체
- [x] `IApiTransmissionManager` 인터페이스 및 구현체
- [x] `IApiAuthenticator` 인터페이스 및 인증 구현체들
- [x] API 응답 모델 클래스들
- [x] 재시도 정책 구현체 (Exponential Backoff 등)
- [x] Failed request 관리 시스템
- [x] HTTP 클라이언트 팩토리 및 설정
- [x] API 통계 및 모니터링 클래스
- [x] API 설정 확장 섹션
- [x] API 연동 관련 단위 테스트

## 완료 조건
1. 설정된 API 엔드포인트로 데이터 전송이 정상 동작함
2. 다양한 인증 방식 (Bearer, Basic, ApiKey)이 지원됨
3. API 전송 실패 시 재시도 정책이 적용됨
4. 배치 전송이 효율적으로 처리됨
5. API 응답 상태 및 오류가 적절히 처리됨
6. Connection pooling으로 성능이 최적화됨
7. API 상태 모니터링 및 통계가 수집됨
8. 모든 API 기능에 대한 단위 테스트가 통과함

## 성능 목표
- 단일 API 호출: < 5초 (타임아웃 포함)
- 동시 API 호출: 최대 10개
- 배치 전송 (100건): < 30초
- 재시도 간격: 1초, 2초, 4초 (지수 백오프)

## 다음 단계 의존성
이 단계가 완료되어야 Step 07 (WPF UI 개발)을 진행할 수 있습니다.

## 예상 소요 시간
**3-4일 (24-32시간)**

## 주의사항
- HttpClient 인스턴스 재사용 (Socket 고갈 방지)
- SSL/TLS 인증서 검증 설정
- 민감한 인증 정보 암호화 저장
- API 호출량 제한 고려 (Rate Limiting)
- 대용량 payload 전송 시 메모리 관리

## 담당자 역할
- **개발자**: API 클라이언트 구현, 인증 시스템 개발
- **보안 담당자**: 인증 방식 및 데이터 전송 보안 검토
- **네트워크 엔지니어**: HTTP 통신 및 프록시 설정 지원
- **검토자**: API 연동 아키텍처 및 오류 처리 검토