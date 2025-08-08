# SimpleSerialToApi 성능 튜닝 가이드

## 1. 개요

이 가이드는 SimpleSerialToApi 애플리케이션의 성능을 최적화하기 위한 구체적인 방법들을 제공합니다. 각 설정의 영향도와 권장값을 포함하여 시스템 환경에 맞는 최적화를 수행할 수 있습니다.

## 2. 성능 벤치마크 기준

### 2.1 성능 목표
- **처리 지연시간**: < 1초 (Serial 수신 → API 전송)
- **처리량**: 1,000+ 메시지/분
- **메모리 사용량**: < 200MB
- **CPU 사용률**: < 50% (평상시)
- **가용성**: 99.9% 이상

### 2.2 측정 도구
```powershell
# 성능 카운터 모니터링
Get-Counter "\Process(SimpleSerialToApi)\% Processor Time"
Get-Counter "\Process(SimpleSerialToApi)\Working Set"
Get-Counter "\.NET CLR Memory(SimpleSerialToApi)\# Bytes in all Heaps"

# 애플리케이션 메트릭 (내장)
# - Queue depth
# - Processing rate (msg/sec)  
# - API response time (ms)
# - Error rate (%)
```

## 3. 메시지 큐 최적화

### 3.1 큐 크기 튜닝

#### 기본 설정
```xml
<add key="Queue.MaxCapacity" value="1000" />
<add key="Queue.BatchSize" value="50" />
<add key="Queue.BatchTimeout" value="5000" />
```

#### 최적화 가이드라인

**고처리량 환경 (>500 msg/min)**
```xml
<add key="Queue.MaxCapacity" value="2000" />
<add key="Queue.BatchSize" value="100" />
<add key="Queue.BatchTimeout" value="3000" />
<add key="Queue.ProcessingThreads" value="4" />
```

**저지연 환경 (< 500ms 응답시간 요구)**
```xml
<add key="Queue.MaxCapacity" value="500" />
<add key="Queue.BatchSize" value="10" />
<add key="Queue.BatchTimeout" value="1000" />
<add key="Queue.ProcessingThreads" value="2" />
```

**메모리 제한 환경 (< 100MB)**
```xml
<add key="Queue.MaxCapacity" value="500" />
<add key="Queue.BatchSize" value="25" />
<add key="Queue.BatchTimeout" value="2000" />
<add key="Queue.CompactOnIdle" value="true" />
```

### 3.2 백압력 제어

#### 동적 큐 관리
```xml
<QueueManagement>
  <!-- 큐 사용률 80% 도달 시 백압력 활성화 -->
  <BackpressureThreshold>0.8</BackpressureThreshold>
  
  <!-- 백압력 시 Serial 수신 속도 조절 -->
  <ThrottleRate>50</ThrottleRate> <!-- 50% 속도 -->
  
  <!-- 큐 사용률 50% 이하 시 정상 속도 복구 -->
  <RecoveryThreshold>0.5</RecoveryThreshold>
</QueueManagement>
```

#### 우선순위 큐 활용
```xml
<PriorityQueue>
  <HighPriority>
    <!-- 알람 데이터 우선 처리 -->
    <Condition>temperature > 80 OR pressure < 900</Condition>
    <Weight>10</Weight>
  </HighPriority>
  <NormalPriority>
    <Condition>default</Condition>
    <Weight>1</Weight>
  </NormalPriority>
</PriorityQueue>
```

### 3.3 큐 모니터링 및 알림

```xml
<QueueMonitoring>
  <Alerts>
    <Alert threshold="80%" action="warning" />
    <Alert threshold="95%" action="critical" />
  </Alerts>
  <Metrics>
    <Metric name="QueueDepth" interval="5000" />
    <Metric name="ProcessingRate" interval="60000" />
    <Metric name="AverageWaitTime" interval="30000" />
  </Metrics>
</QueueMonitoring>
```

## 4. API 통신 최적화

### 4.1 연결 관리

#### HTTP 클라이언트 풀링
```xml
<HttpClientPool>
  <MaxConnections>10</MaxConnections>
  <ConnectionTimeout>30000</ConnectionTimeout>
  <RequestTimeout>15000</RequestTimeout>
  <KeepAliveTimeout>300000</KeepAliveTimeout>
  <MaxConnectionsPerEndpoint>5</MaxConnectionsPerEndpoint>
</HttpClientPool>
```

#### 연결 재사용 최적화
```csharp
// HttpClient 설정 예제
services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.ConnectionClose = false; // Keep-Alive
    client.DefaultRequestHeaders.Add("User-Agent", "SimpleSerialToApi/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 5,
    UseProxy = false, // 프록시 사용 안 함으로 성능 향상
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
});
```

### 4.2 배치 처리 최적화

#### 동적 배치 크기 조정
```xml
<DynamicBatching>
  <!-- 낮은 부하 시 작은 배치로 지연시간 최소화 -->
  <LowLoad threshold="10" batchSize="5" timeout="1000" />
  
  <!-- 보통 부하 시 균형된 설정 -->
  <MediumLoad threshold="50" batchSize="25" timeout="3000" />
  
  <!-- 높은 부하 시 큰 배치로 처리량 극대화 -->
  <HighLoad threshold="100" batchSize="100" timeout="1000" />
</DynamicBatching>
```

#### 병렬 API 호출
```xml
<ParallelProcessing>
  <MaxConcurrentRequests>5</MaxConcurrentRequests>
  <MaxConcurrentBatches>3</MaxConcurrentBatches>
  <ThreadPoolMinThreads>10</ThreadPoolMinThreads>
  <ThreadPoolMaxThreads>50</ThreadPoolMaxThreads>
</ParallelProcessing>
```

### 4.3 재시도 정책 최적화

#### 지수 백오프 조정
```xml
<RetryPolicy>
  <MaxAttempts>3</MaxAttempts>
  <BaseDelay>500</BaseDelay>    <!-- 첫 재시도: 500ms -->
  <MaxDelay>10000</MaxDelay>    <!-- 최대 재시도 간격: 10초 -->
  <Multiplier>2.0</Multiplier>  <!-- 지수 증가율 -->
  <Jitter>true</Jitter>         <!-- 랜덤 지연 추가로 thundering herd 방지 -->
</RetryPolicy>
```

#### 회로 차단기 패턴
```xml
<CircuitBreaker>
  <FailureThreshold>5</FailureThreshold>      <!-- 5회 연속 실패 시 회로 차단 -->
  <TimeoutDuration>30000</TimeoutDuration>    <!-- 30초 후 반개방 상태 -->
  <SuccessThreshold>3</SuccessThreshold>      <!-- 3회 성공 시 회로 닫힘 -->
</CircuitBreaker>
```

## 5. Serial 통신 최적화

### 5.1 버퍼 크기 튜닝

#### 고속 데이터 스트림 (115200 bps+)
```xml
<SerialOptimization>
  <ReadBufferSize>8192</ReadBufferSize>     <!-- 8KB 읽기 버퍼 -->
  <WriteBufferSize>4096</WriteBufferSize>   <!-- 4KB 쓰기 버퍼 -->
  <ReceivedBytesThreshold>512</ReceivedBytesThreshold>  <!-- 512바이트마다 이벤트 -->
</SerialOptimization>
```

#### 저속 데이터 스트림 (9600 bps)
```xml
<SerialOptimization>
  <ReadBufferSize>2048</ReadBufferSize>     <!-- 2KB 읽기 버퍼 -->
  <WriteBufferSize>1024</WriteBufferSize>   <!-- 1KB 쓰기 버퍼 -->
  <ReceivedBytesThreshold>1</ReceivedBytesThreshold>    <!-- 즉시 처리 -->
</SerialOptimization>
```

### 5.2 데이터 처리 최적화

#### 파싱 성능 향상
```csharp
// 정규표현식 컴파일 및 캐싱
public class OptimizedParser
{
    private static readonly Regex CompiledPattern = 
        new Regex(@"TEMP:(\d+\.\d+),HUM:(\d+\.\d+)", 
                  RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // StringBuilder 재사용으로 메모리 할당 최소화
    private readonly StringBuilder _stringBuilder = new StringBuilder(256);
    
    // ArrayPool 사용으로 GC 압박 감소
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
}
```

#### 비동기 처리 파이프라인
```csharp
public class AsyncDataPipeline
{
    private readonly Channel<byte[]> _rawDataChannel;
    private readonly Channel<ParsedData> _parsedDataChannel;
    
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var parsingTask = Task.Run(async () =>
        {
            await foreach (var rawData in _rawDataChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var parsed = await ParseAsync(rawData);
                await _parsedDataChannel.Writer.WriteAsync(parsed, cancellationToken);
            }
        });
        
        var apiTask = Task.Run(async () =>
        {
            await foreach (var parsedData in _parsedDataChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await SendToApiAsync(parsedData);
            }
        });
        
        await Task.WhenAll(parsingTask, apiTask);
    }
}
```

## 6. 메모리 관리 최적화

### 6.1 가비지 컬렉션 튜닝

#### 서버 GC 설정 (고처리량 환경)
```xml
<runtime>
  <gcServer enabled="true" />
  <gcConcurrent enabled="true" />
  <GCRetainVM enabled="true" />
  <GCNoAffinitize enabled="false" />
  <GCHeapCount enabled="0" />  <!-- CPU 코어 수만큼 힙 생성 -->
</runtime>
```

#### 워크스테이션 GC 설정 (저지연 환경)
```xml
<runtime>
  <gcServer enabled="false" />
  <gcConcurrent enabled="true" />
  <GCLatencyMode>Interactive</GCLatencyMode>
</runtime>
```

### 6.2 메모리 풀링

#### 객체 풀링
```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _objectGenerator;
    
    public T Get()
    {
        if (_objects.TryDequeue(out var item))
            return item;
        
        return _objectGenerator();
    }
    
    public void Return(T item)
    {
        if (item != null)
            _objects.Enqueue(item);
    }
}

// 사용 예제
private readonly ObjectPool<ParsedData> _parsedDataPool = 
    new ObjectPool<ParsedData>(() => new ParsedData());
```

#### 버퍼 풀링
```csharp
// ArrayPool 사용으로 메모리 할당 최소화
private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

public void ProcessData(int dataLength)
{
    var buffer = _bufferPool.Rent(dataLength);
    try
    {
        // 데이터 처리
    }
    finally
    {
        _bufferPool.Return(buffer, clearArray: true);
    }
}
```

### 6.3 메모리 모니터링

```xml
<MemoryMonitoring>
  <Thresholds>
    <Warning>150MB</Warning>    <!-- 150MB 초과 시 경고 -->
    <Critical>200MB</Critical>  <!-- 200MB 초과 시 위험 -->
  </Thresholds>
  <Actions>
    <OnWarning>reduce_queue_size,increase_gc_frequency</OnWarning>
    <OnCritical>force_gc,restart_if_necessary</OnCritical>
  </Actions>
</MemoryMonitoring>
```

## 7. 로깅 성능 최적화

### 7.1 로그 레벨 최적화

#### 운영 환경 설정
```xml
<add key="Serilog.MinimumLevel" value="Information" />
<add key="Serilog.Override.Microsoft" value="Warning" />
<add key="Serilog.Override.System" value="Warning" />
```

#### 성능 측정 환경 설정
```xml
<add key="Serilog.MinimumLevel" value="Warning" />
<add key="Serilog.Override.SimpleSerialToApi.Performance" value="Debug" />
```

### 7.2 비동기 로깅

```xml
<add key="Serilog.WriteTo.Async.Configure" value="Serilog.Sinks.File" />
<add key="Serilog.WriteTo.Async.BufferSize" value="10000" />
<add key="Serilog.WriteTo.Async.BlockWhenFull" value="false" />
```

### 7.3 구조화된 로깅 최적화

```csharp
// 문자열 보간 대신 구조화된 로그 사용
// ❌ 비효율적
_logger.LogInformation($"Processing data from {deviceId} at {timestamp}");

// ✅ 효율적
_logger.LogInformation("Processing data from {DeviceId} at {Timestamp}", deviceId, timestamp);
```

## 8. 시스템 레벨 최적화

### 8.1 프로세스 우선순위 설정

```csharp
// 애플리케이션 시작 시 우선순위 설정
Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
```

### 8.2 스레드 풀 최적화

```csharp
// 애플리케이션 시작 시 스레드 풀 설정
ThreadPool.SetMinThreads(workerThreads: 20, completionPortThreads: 20);
ThreadPool.SetMaxThreads(workerThreads: 100, completionPortThreads: 100);
```

### 8.3 네트워크 최적화

#### TCP 설정 최적화 (Windows)
```cmd
# TCP Chimney Offload 활성화
netsh int tcp set global chimney=enabled

# TCP 윈도우 스케일링 활성화  
netsh int tcp set global autotuninglevel=normal

# TCP RSS 활성화
netsh int tcp set global rss=enabled
```

#### DNS 캐시 최적화
```xml
<system.net>
  <settings>
    <servicePointManager checkCertificateName="false" 
                        checkCertificateRevocationList="false" />
  </settings>
  <connectionManagement>
    <add address="*" maxconnection="10" />
  </connectionManagement>
</system.net>
```

## 9. 모니터링 및 성능 측정

### 9.1 키 성능 지표 (KPI)

```csharp
public class PerformanceMetrics
{
    // 처리량 메트릭
    public int MessagesPerSecond { get; set; }
    public int TotalMessagesProcessed { get; set; }
    
    // 지연시간 메트릭
    public double AverageProcessingTime { get; set; }
    public double P95ProcessingTime { get; set; }
    public double P99ProcessingTime { get; set; }
    
    // 리소스 사용률
    public double CpuUsagePercentage { get; set; }
    public long MemoryUsageBytes { get; set; }
    public int ActiveConnections { get; set; }
    
    // 오류율
    public double ErrorRate { get; set; }
    public int TotalErrors { get; set; }
}
```

### 9.2 성능 프로파일링

```csharp
// 성능 측정 헬퍼
public class PerformanceProfiler : IDisposable
{
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly ILogger _logger;
    
    public PerformanceProfiler(string operationName, ILogger logger)
    {
        _operationName = operationName;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogDebug("Operation {Operation} completed in {Duration}ms", 
            _operationName, _stopwatch.ElapsedMilliseconds);
    }
}

// 사용 예제
using (new PerformanceProfiler("DataParsing", _logger))
{
    var result = ParseData(rawData);
}
```

## 10. 성능 튜닝 체크리스트

### 10.1 초기 설정 (신규 설치 시)
- [ ] 하드웨어 사양 확인 및 최적 설정 적용
- [ ] .NET 8 최신 버전 설치
- [ ] Windows 성능 옵션 설정 (시각적 효과 끄기)
- [ ] 안티바이러스 실시간 검사 예외 등록

### 10.2 애플리케이션 설정
- [ ] 큐 크기 환경에 맞게 조정
- [ ] API 배치 처리 설정 최적화
- [ ] 로그 레벨 운영 환경에 맞게 설정
- [ ] 메모리 관리 정책 적용

### 10.3 시스템 최적화
- [ ] 스레드 풀 설정 조정
- [ ] 네트워크 설정 최적화
- [ ] 가비지 컬렉션 튜닝 적용
- [ ] 프로세스 우선순위 설정

### 10.4 모니터링 설정
- [ ] 성능 카운터 모니터링 설정
- [ ] 알림 임계값 설정
- [ ] 성능 로그 수집 활성화
- [ ] 정기적인 성능 리포트 생성

## 11. 환경별 권장 설정

### 11.1 개발 환경
```xml
<!-- 빠른 피드백을 위한 설정 -->
<add key="Queue.MaxCapacity" value="100" />
<add key="Queue.BatchSize" value="5" />
<add key="Serilog.MinimumLevel" value="Debug" />
<add key="API.Timeout" value="10000" />
```

### 11.2 테스트 환경
```xml
<!-- 부하 테스트를 위한 설정 -->
<add key="Queue.MaxCapacity" value="1000" />
<add key="Queue.BatchSize" value="50" />
<add key="Serilog.MinimumLevel" value="Information" />
<add key="API.Timeout" value="30000" />
```

### 11.3 운영 환경
```xml
<!-- 안정성과 성능 균형 -->
<add key="Queue.MaxCapacity" value="2000" />
<add key="Queue.BatchSize" value="100" />
<add key="Serilog.MinimumLevel" value="Warning" />
<add key="API.Timeout" value="15000" />
<add key="Monitoring.Enabled" value="true" />
```

이 가이드를 참조하여 환경과 요구사항에 맞는 성능 최적화를 수행하시기 바랍니다. 성능 변경 후에는 반드시 모니터링을 통해 효과를 검증해야 합니다.