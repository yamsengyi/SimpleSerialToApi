# Step 05: 내부 Message Queue 구현

## 개요
Serial 수신 이벤트와 API 전송 사이의 버퍼 역할을 하는 내부 Message Queue 시스템을 구현합니다. 고성능 처리와 장애 복구 기능을 포함합니다.

## 상세 작업

### 5.1 Message Queue 아키텍처 설계
- `IMessageQueue<T>` 제네릭 인터페이스 정의
- `ConcurrentMessageQueue` 구현 (thread-safe)
- Priority Queue 기능 (우선순위 기반 처리)
- 배치 처리 및 bulk operation 지원

### 5.2 Queue 메시지 모델
```csharp
public class QueueMessage<T>
{
    public string MessageId { get; set; }
    public T Payload { get; set; }
    public DateTime EnqueueTime { get; set; }
    public DateTime? ProcessingStartTime { get; set; }
    public int RetryCount { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public MessageStatus Status { get; set; }
}

public enum MessageStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    DeadLetter
}
```

### 5.3 재시도 정책 구현
- 지수 백오프 (Exponential Backoff) 재시도
- 최대 재시도 횟수 제한
- 재시도 간격 설정 가능
- Dead Letter Queue 구현 (처리 실패 메시지)

### 5.4 Queue 프로세서
```csharp
public interface IQueueProcessor<T>
{
    Task<ProcessingResult> ProcessAsync(QueueMessage<T> message);
    Task<BatchProcessingResult> ProcessBatchAsync(List<QueueMessage<T>> messages);
    bool CanProcess(QueueMessage<T> message);
    int MaxBatchSize { get; }
}

public class ApiDataQueueProcessor : IQueueProcessor<MappedApiData>
{
    // API 전송 처리 로직
}
```

### 5.5 Queue 관리자
- `IQueueManager` 인터페이스
- Queue 생성/삭제/관리
- Queue 상태 모니터링
- Queue 통계 및 성능 메트릭 수집

### 5.6 App.Config Queue 설정
```xml
<messageQueue>
  <queues>
    <add name="ApiDataQueue"
         maxSize="1000"
         batchSize="10"
         batchTimeout="5000"
         retryCount="3"
         retryInterval="5000"
         enablePriority="true" />
    <add name="LogQueue"
         maxSize="500"
         batchSize="20"
         batchTimeout="10000"
         retryCount="1"
         retryInterval="1000"
         enablePriority="false" />
  </queues>
  <processors>
    <add queueName="ApiDataQueue"
         processorType="SimpleSerialToApi.Services.ApiDataQueueProcessor"
         threadCount="2"
         enableAsync="true" />
  </processors>
</messageQueue>
```

### 5.7 Queue 모니터링 및 메트릭
- Queue 크기 모니터링
- 처리 속도 및 지연시간 측정
- 실패율 추적
- 성능 카운터 제공

## 기술 요구사항
- System.Collections.Concurrent 활용
- Task Parallel Library (TPL)
- 비동기/병렬 처리
- 메모리 효율적인 구조

## 주요 클래스 및 인터페이스

### IMessageQueue<T>
```csharp
public interface IMessageQueue<T>
{
    Task<bool> EnqueueAsync(QueueMessage<T> message);
    Task<QueueMessage<T>> DequeueAsync(CancellationToken cancellationToken = default);
    Task<List<QueueMessage<T>>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<bool> RequeueAsync(QueueMessage<T> message);
    
    int Count { get; }
    int ProcessingCount { get; }
    bool IsEmpty { get; }
    QueueStatistics GetStatistics();
}
```

### IQueueManager
```csharp
public interface IQueueManager
{
    IMessageQueue<T> CreateQueue<T>(string queueName, QueueConfiguration config);
    IMessageQueue<T> GetQueue<T>(string queueName);
    Task StartProcessingAsync(string queueName);
    Task StopProcessingAsync(string queueName);
    QueueHealthStatus GetQueueHealth(string queueName);
    Task<QueueStatistics> GetAllQueueStatisticsAsync();
}
```

### QueueConfiguration
```csharp
public class QueueConfiguration
{
    public int MaxSize { get; set; } = 1000;
    public int BatchSize { get; set; } = 10;
    public int BatchTimeoutMs { get; set; } = 5000;
    public int RetryCount { get; set; } = 3;
    public int RetryIntervalMs { get; set; } = 5000;
    public bool EnablePriority { get; set; } = false;
    public int ProcessorThreadCount { get; set; } = 1;
}
```

## 산출물
- [x] `IMessageQueue<T>` 인터페이스 및 구현체
- [x] `IQueueProcessor<T>` 인터페이스 및 API 전송 프로세서
- [x] `IQueueManager` 인터페이스 및 구현체
- [x] Queue 메시지 모델 및 상태 열거형
- [x] 재시도 정책 구현체
- [x] Dead Letter Queue 구현
- [x] Queue 통계 및 모니터링 클래스
- [x] Queue 설정 섹션 및 모델
- [x] Queue 관련 단위 테스트

## 완료 조건
1. 1000건 이상의 메시지를 동시에 처리할 수 있음
2. 메시지 처리 실패 시 설정된 재시도 정책이 동작함
3. Dead Letter Queue에 처리 불가능한 메시지가 저장됨
4. 배치 처리가 정상적으로 동작함
5. Queue 통계 및 성능 메트릭이 정확히 수집됨
6. 멀티스레드 환경에서 thread-safe하게 동작함
7. 메모리 사용량이 안정적임 (메모리 리크 없음)
8. 모든 Queue 기능에 대한 단위 테스트가 통과함

## 성능 목표
- 초당 100건 이상 메시지 처리
- 메시지 지연시간: < 100ms (평균)
- Queue 가득참 상황에서의 백프레셔 처리
- 메모리 사용량: < 200MB (1000건 대기열 시)

## 다음 단계 의존성
이 단계가 완료되어야 Step 06 (API 연동 계층)을 진행할 수 있습니다.

## 예상 소요 시간
**3-4일 (24-32시간)**

## 주의사항
- ConcurrentQueue의 성능 특성 이해 및 활용
- 메모리 압박 상황에서의 Queue 크기 제한
- 장기간 실행 시 GC 압박 최소화
- 프로세서 스레드 수 최적화

## 담당자 역할
- **개발자**: Queue 시스템 구현, 프로세서 개발
- **성능 엔지니어**: 동시성 및 성능 최적화
- **시스템 엔지니어**: 모니터링 및 알림 설계
- **검토자**: 동시성 및 장애 복구 로직 검토