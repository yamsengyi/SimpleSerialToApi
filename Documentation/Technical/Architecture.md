# SimpleSerialToApi 시스템 아키텍처

## 1. 전체 시스템 개요

### 1.1 시스템 목적
SimpleSerialToApi는 Serial 통신을 통해 하드웨어 장비로부터 데이터를 수집하고, 이를 파싱하여 REST API로 전송하는 중개 애플리케이션입니다.

### 1.2 핵심 특징
- **실시간 데이터 처리**: 고속 Serial 데이터 스트림 처리
- **확장성**: 다양한 장비 유형 및 API 프로토콜 지원
- **안정성**: 메시지 큐 기반의 장애 복구 메커니즘
- **모니터링**: 실시간 상태 모니터링 및 로깅
- **설정 관리**: 유연한 구성 관리 시스템

## 2. 전체 구조도

```
┌─────────────────────────────────────────────────────────────────┐
│                        SimpleSerialToApi                        │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐ │
│  │  Presentation   │    │   Application   │    │  Infrastructure │ │
│  │     Layer       │    │     Layer       │    │      Layer     │ │
│  │                 │    │                 │    │                 │ │
│  │ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │ │
│  │ │ WPF Views   │ │    │ │   Services  │ │    │ │   Serial    │ │ │
│  │ │ ViewModels  │ │◄──►│ │ Controllers │ │◄──►│ │ API Clients │ │ │
│  │ │ Converters  │ │    │ │ Validators  │ │    │ │   Logging   │ │ │
│  │ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │ │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐ │
│  │   Domain        │    │   Shared        │    │   External      │ │
│  │   Layer         │    │   Kernel        │    │   Systems       │ │
│  │                 │    │                 │    │                 │ │
│  │ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │ │
│  │ │   Models    │ │    │ │   Common    │ │    │ │ Serial      │ │ │
│  │ │ Interfaces  │ │◄──►│ │ Extensions  │ │◄──►│ │ Devices     │ │ │
│  │ │  Services   │ │    │ │ Utilities   │ │    │ │ REST APIs   │ │ │
│  │ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │ │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## 3. 주요 컴포넌트

### 3.1 Serial Communication Service
```csharp
┌─────────────────────────────────────┐
│      Serial Communication          │
├─────────────────────────────────────┤
│ • ISerialCommunicationService       │
│ • SerialPortManager                 │
│ • DataReceiver                      │
│ • ConnectionMonitor                 │
│                                     │
│ Functions:                          │
│ ├─ Port Management                  │
│ ├─ Data Reception                   │
│ ├─ Connection Monitoring            │
│ ├─ Error Handling                   │
│ └─ Reconnection Logic               │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│         Raw Serial Data             │
└─────────────────────────────────────┘
```

**주요 책임:**
- Serial 포트 관리 및 연결 제어
- 원시 데이터 수신 및 버퍼링
- 연결 상태 모니터링 및 자동 재연결
- 오류 감지 및 복구

**기술적 세부사항:**
- `System.IO.Ports.SerialPort` 사용
- 비동기 데이터 수신 (`DataReceived` 이벤트)
- Connection pooling 및 resource cleanup
- Timeout 및 retry 메커니즘

### 3.2 Data Parsing Engine
```csharp
┌─────────────────────────────────────┐
│         Data Parsing Engine         │
├─────────────────────────────────────┤
│ • IDataParser                       │
│ • ParserFactory                     │
│ • DeviceSpecificParsers             │
│ • ValidationRules                   │
│                                     │
│ Supported Formats:                  │
│ ├─ ASCII Text                       │
│ ├─ Binary Protocols                 │
│ ├─ CSV Format                       │
│ ├─ JSON Strings                     │
│ └─ Custom Protocols                 │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│       Structured Data Objects      │
└─────────────────────────────────────┘
```

**파싱 전략:**
- **정규표현식 기반**: 패턴 매칭을 통한 데이터 추출
- **상태 기반 파싱**: 복잡한 프로토콜을 위한 상태 머신
- **플러그인 아키텍처**: 새로운 장비 유형 쉽게 추가 가능

### 3.3 Message Queue System
```csharp
┌─────────────────────────────────────┐
│        Message Queue System        │
├─────────────────────────────────────┤
│ • IMessageQueue                     │
│ • InMemoryQueue                     │
│ • PersistentQueue (Optional)        │
│ • QueueProcessor                    │
│                                     │
│ Features:                           │
│ ├─ FIFO Processing                  │
│ ├─ Priority Queues                  │
│ ├─ Dead Letter Queue                │
│ ├─ Batch Processing                 │
│ └─ Flow Control                     │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│          API Client Service         │
└─────────────────────────────────────┘
```

**큐 관리 전략:**
- **메모리 기반**: 고성능 처리를 위한 In-Memory 큐
- **백압력 제어**: 큐 오버플로우 방지
- **배치 처리**: 여러 메시지를 묶어서 API 호출 최적화
- **재시도 큐**: 실패한 메시지의 별도 처리

### 3.4 API Client Service
```csharp
┌─────────────────────────────────────┐
│          API Client Service         │
├─────────────────────────────────────┤
│ • IApiClient                        │
│ • HttpClientManager                 │
│ • AuthenticationHandler             │
│ • RetryPolicyHandler                │
│                                     │
│ Supported Auth:                     │
│ ├─ Bearer Token                     │
│ ├─ Basic Authentication             │
│ ├─ API Key                          │
│ ├─ OAuth 2.0                        │
│ └─ Custom Headers                   │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│           REST API Server           │
└─────────────────────────────────────┘
```

**HTTP 클라이언트 관리:**
- **Connection Pooling**: 효율적인 연결 재사용
- **Timeout 관리**: Request/Response timeout 설정
- **압축 지원**: GZip/Deflate 압축 전송
- **SSL/TLS**: 보안 연결 지원

### 3.5 Configuration Manager
```csharp
┌─────────────────────────────────────┐
│       Configuration Manager         │
├─────────────────────────────────────┤
│ • IConfigurationService             │
│ • AppConfigProvider                 │
│ • JsonConfigProvider                │
│ • EnvironmentConfigProvider         │
│                                     │
│ Configuration Sources:              │
│ ├─ App.config (Primary)             │
│ ├─ appsettings.json                 │
│ ├─ Environment Variables            │
│ ├─ Command Line Args                │
│ └─ External Config Server           │
└─────────────────────────────────────┘
```

**설정 관리 계층:**
1. **기본 설정**: 애플리케이션 기본값
2. **파일 설정**: App.config, JSON 파일
3. **환경 변수**: 배포 환경별 설정
4. **런타임 설정**: 사용자 인터페이스를 통한 동적 설정

### 3.6 UI Layer (WPF)
```csharp
┌─────────────────────────────────────┐
│             UI Layer (WPF)          │
├─────────────────────────────────────┤
│ Views:                              │
│ ├─ MainWindow                       │
│ ├─ SettingsWindow                   │
│ ├─ LogViewer                        │
│ ├─ DataMonitor                      │
│ └─ StatusDashboard                  │
│                                     │
│ ViewModels (MVVM):                  │
│ ├─ MainViewModel                    │
│ ├─ SettingsViewModel                │
│ ├─ LogViewerViewModel               │
│ └─ DataMonitorViewModel             │
│                                     │
│ Services:                           │
│ ├─ DialogService                    │
│ ├─ NavigationService                │
│ └─ NotificationService              │
└─────────────────────────────────────┘
```

## 4. 데이터 흐름

### 4.1 전체 데이터 흐름도
```
Serial Device → Raw Data → Parser → Validator → Queue → Batch Processor → API Client → REST API
     │              │         │         │         │            │              │           │
     ▼              ▼         ▼         ▼         ▼            ▼              ▼           ▼
  Physical      Byte Stream  Structured  Valid   Message    Batch of      HTTP Request  JSON
  Connection      Buffer     Data Object Object   Queue      Messages       Payload    Response
                    │                                          │
                    ▼                                          ▼
               Error Handler                               Response Handler
                    │                                          │
                    ▼                                          ▼
               Log & Retry                                Log & Status Update
```

### 4.2 상세 처리 단계

#### 단계 1: 데이터 수신
```csharp
Serial Device → SerialPort → DataReceived Event → Raw Buffer
```
- 비동기 이벤트 기반 수신
- 바이트 스트림 버퍼링
- 라인 구분자 감지

#### 단계 2: 데이터 파싱
```csharp
Raw Buffer → Parser → Data Validation → Structured Object
```
- 장비별 파싱 규칙 적용
- 데이터 유효성 검증
- 구조화된 데이터 객체 생성

#### 단계 3: 큐잉
```csharp
Structured Object → Message Queue → Batch Processor
```
- 우선순위 기반 큐잉
- 배치 크기 및 타임아웃 관리
- 백압력 제어

#### 단계 4: API 전송
```csharp
Batch → JSON Serialization → HTTP Client → API Server
```
- JSON 직렬화
- HTTP 요청 생성 및 전송
- 응답 처리 및 재시도

## 5. 아키텍처 패턴

### 5.1 적용된 디자인 패턴

#### Repository Pattern
```csharp
public interface IConfigurationRepository
{
    Task<Configuration> GetAsync();
    Task SaveAsync(Configuration config);
    Task<bool> ValidateAsync(Configuration config);
}

public class AppConfigRepository : IConfigurationRepository
{
    // App.config 기반 구현
}
```

#### Strategy Pattern
```csharp
public interface IDataParser
{
    ParseResult Parse(byte[] rawData);
    bool CanParse(DeviceType deviceType);
}

public class TemperatureSensorParser : IDataParser { }
public class PressureSensorParser : IDataParser { }
```

#### Observer Pattern
```csharp
public interface IDataProcessor
{
    event EventHandler<DataReceivedEventArgs> DataReceived;
    event EventHandler<ErrorEventArgs> ErrorOccurred;
}
```

#### Command Pattern
```csharp
public interface ICommand
{
    Task<CommandResult> ExecuteAsync();
    Task<CommandResult> UndoAsync();
}

public class ConnectSerialCommand : ICommand { }
public class DisconnectSerialCommand : ICommand { }
```

### 5.2 SOLID 원칙 적용

#### Single Responsibility Principle
- 각 클래스는 단일 책임만 가짐
- `SerialCommunicationService`: Serial 통신만 담당
- `DataParsingService`: 데이터 파싱만 담당

#### Open/Closed Principle
- 새로운 장비 타입 추가 시 기존 코드 수정 없이 확장 가능
- `IDataParser` 인터페이스를 통한 새로운 파서 추가

#### Liskov Substitution Principle
- 파생 클래스는 기본 클래스를 완전히 대체 가능
- 모든 `IDataParser` 구현체는 동일한 방식으로 사용 가능

#### Interface Segregation Principle
- 세분화된 인터페이스 제공
- `IReadOnlyConfiguration`, `IWritableConfiguration` 분리

#### Dependency Inversion Principle
- 상위 레벨 모듈이 하위 레벨 모듈에 의존하지 않음
- 의존성 주입을 통한 느슨한 결합

## 6. 성능 고려사항

### 6.1 메모리 관리
```csharp
// 메모리 풀 사용으로 GC 압박 감소
private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

// 대용량 객체 재사용
private readonly ConcurrentQueue<ParsedData> _objectPool = new();
```

### 6.2 비동기 처리
```csharp
// 비동기 스트림 처리
public async IAsyncEnumerable<ParsedData> ProcessDataStreamAsync(
    IAsyncEnumerable<byte[]> dataStream, 
    CancellationToken cancellationToken = default)
{
    await foreach (var chunk in dataStream.WithCancellation(cancellationToken))
    {
        var parsed = await ParseAsync(chunk);
        yield return parsed;
    }
}
```

### 6.3 캐싱 전략
```csharp
// 파싱 규칙 캐싱
private readonly IMemoryCache _parsingRulesCache;

// API 응답 캐싱 (필요시)
private readonly IDistributedCache _responseCache;
```

## 7. 보안 아키텍처

### 7.1 인증 및 권한부여
```csharp
┌─────────────────────────────────────┐
│          Security Layer             │
├─────────────────────────────────────┤
│ • Authentication Service            │
│ • Authorization Service             │
│ • Token Management                  │
│ • Certificate Handling              │
│                                     │
│ Security Features:                  │
│ ├─ Role-based Access Control        │
│ ├─ API Token Encryption             │
│ ├─ SSL/TLS Communication            │
│ ├─ Input Validation                 │
│ └─ Audit Logging                    │
└─────────────────────────────────────┘
```

### 7.2 데이터 보호
- **전송 중 암호화**: HTTPS/TLS 1.2+
- **저장 암호화**: 민감한 설정 정보 암호화
- **입력 검증**: SQL Injection, XSS 방지
- **감사 로깅**: 모든 보안 관련 이벤트 로깅

## 8. 확장성 고려사항

### 8.1 수평적 확장
- **Multiple Instances**: 여러 Serial 포트 동시 처리
- **Load Balancing**: 여러 API 엔드포인트 분산
- **Message Queue 확장**: External Queue (Redis, RabbitMQ) 지원

### 8.2 수직적 확장
- **Threading**: 멀티스레드 데이터 처리
- **Memory**: 대용량 메모리 활용을 위한 메모리 관리
- **CPU**: CPU 집약적 파싱 작업 최적화

## 9. 운영 및 모니터링

### 9.1 Health Check
```csharp
public class SystemHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var isSerialConnected = await CheckSerialConnection();
        var isApiReachable = await CheckApiConnection();
        var queueHealth = CheckQueueStatus();
        
        return isSerialConnected && isApiReachable && queueHealth 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy();
    }
}
```

### 9.2 메트릭 수집
- **Application Metrics**: 처리량, 응답시간, 오류율
- **System Metrics**: CPU, 메모리, 디스크 사용률
- **Business Metrics**: 데이터 수집 성공률, API 호출 성공률

## 10. 기술 스택 요약

### 10.1 Core Technologies
- **.NET 8**: 기본 런타임 및 프레임워크
- **WPF**: 사용자 인터페이스
- **System.IO.Ports**: Serial 통신
- **HttpClient**: HTTP API 통신

### 10.2 Third-party Libraries
- **Serilog**: 구조화된 로깅
- **Newtonsoft.Json**: JSON 직렬화
- **Polly**: Retry 및 Circuit Breaker 패턴
- **Microsoft.Extensions.*****: DI, Configuration, Hosting

### 10.3 Development Tools
- **Visual Studio 2022**: 주 개발 환경
- **WiX Toolset**: MSI 설치 패키지 생성
- **NUnit/xUnit**: 단위 테스트
- **Moq**: Mock 객체 생성

이 아키텍처는 확장성, 유지보수성, 성능을 고려하여 설계되었으며, 기업 환경에서의 안정적인 운영을 목표로 합니다.