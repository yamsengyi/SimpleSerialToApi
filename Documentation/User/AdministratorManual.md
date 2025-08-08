# SimpleSerialToApi 관리자 매뉴얼

## 1. 개요
이 문서는 SimpleSerialToApi의 시스템 관리자를 위한 상세 설정 가이드, 모니터링 방법, 유지보수 절차를 다룹니다.

## 2. App.Config 설정 상세

### 2.1 Serial 통신 설정

#### 기본 Serial 설정
```xml
<appSettings>
  <!-- Serial Port Configuration -->
  <add key="Serial.PortName" value="COM1" />
  <add key="Serial.BaudRate" value="9600" />
  <add key="Serial.DataBits" value="8" />
  <add key="Serial.Parity" value="None" />
  <add key="Serial.StopBits" value="One" />
  <add key="Serial.Handshake" value="None" />
  <add key="Serial.ReadTimeout" value="5000" />
  <add key="Serial.WriteTimeout" value="5000" />
</appSettings>
```

#### 고급 Serial 설정
```xml
<appSettings>
  <!-- Advanced Serial Configuration -->
  <add key="Serial.RtsEnable" value="true" />
  <add key="Serial.DtrEnable" value="true" />
  <add key="Serial.ReceivedBytesThreshold" value="1" />
  <add key="Serial.ReadBufferSize" value="4096" />
  <add key="Serial.WriteBufferSize" value="2048" />
  <add key="Serial.DiscardNull" value="false" />
  
  <!-- Connection Management -->
  <add key="Serial.AutoReconnect" value="true" />
  <add key="Serial.ReconnectInterval" value="10000" />
  <add key="Serial.MaxReconnectAttempts" value="10" />
  
  <!-- Data Processing -->
  <add key="Serial.DataFormat" value="ASCII" />
  <add key="Serial.LineEnding" value="CRLF" />
  <add key="Serial.IgnoreEmptyLines" value="true" />
</appSettings>
```

#### 다중 포트 설정 (Enterprise Edition)
```xml
<SerialPorts>
  <Port name="Primary">
    <PortName>COM1</PortName>
    <BaudRate>9600</BaudRate>
    <DeviceType>TemperatureSensor</DeviceType>
    <Enabled>true</Enabled>
  </Port>
  <Port name="Secondary">
    <PortName>COM2</PortName>
    <BaudRate>115200</BaudRate>
    <DeviceType>PressureSensor</DeviceType>
    <Enabled>false</Enabled>
  </Port>
</SerialPorts>
```

### 2.2 API 매핑 구성

#### 기본 API 설정
```xml
<appSettings>
  <!-- API Configuration -->
  <add key="API.BaseUrl" value="https://api.yourcompany.com" />
  <add key="API.Endpoint" value="/sensors/data" />
  <add key="API.Method" value="POST" />
  <add key="API.ContentType" value="application/json" />
  <add key="API.Timeout" value="30000" />
  
  <!-- Authentication -->
  <add key="API.AuthType" value="Bearer" />
  <add key="API.AuthToken" value="your-secret-token" />
  
  <!-- Headers -->
  <add key="API.Headers.UserAgent" value="SimpleSerialToApi/1.0" />
  <add key="API.Headers.Accept" value="application/json" />
</appSettings>
```

#### 다중 API 엔드포인트 설정
```xml
<ApiEndpoints>
  <Endpoint name="Primary">
    <Url>https://api.primary.com/data</Url>
    <Method>POST</Method>
    <AuthType>Bearer</AuthType>
    <AuthToken>primary-token</AuthToken>
    <Enabled>true</Enabled>
    <Priority>1</Priority>
  </Endpoint>
  <Endpoint name="Backup">
    <Url>https://api.backup.com/data</Url>
    <Method>POST</Method>
    <AuthType>Bearer</AuthType>
    <AuthToken>backup-token</AuthToken>
    <Enabled>true</Enabled>
    <Priority>2</Priority>
  </Endpoint>
</ApiEndpoints>
```

#### 재시도 정책 설정
```xml
<RetryPolicy>
  <MaxAttempts>3</MaxAttempts>
  <InitialDelay>1000</InitialDelay>
  <MaxDelay>30000</MaxDelay>
  <BackoffMultiplier>2.0</BackoffMultiplier>
  <RetryOn>
    <HttpStatusCode>500</HttpStatusCode>
    <HttpStatusCode>502</HttpStatusCode>
    <HttpStatusCode>503</HttpStatusCode>
    <HttpStatusCode>504</HttpStatusCode>
  </RetryOn>
</RetryPolicy>
```

### 2.3 데이터 매핑 규칙

#### JSON 템플릿 매핑
```xml
<DataMappings>
  <Mapping name="TemperatureSensor">
    <Pattern>^TEMP:(\d+\.\d+),HUM:(\d+\.\d+)$</Pattern>
    <JsonTemplate><![CDATA[
    {
      "deviceId": "TEMP_001",
      "timestamp": "{timestamp}",
      "temperature": {group1},
      "humidity": {group2},
      "location": "Building A"
    }
    ]]></JsonTemplate>
    <Validation>
      <MinValue field="temperature">-50</MinValue>
      <MaxValue field="temperature">100</MaxValue>
      <MinValue field="humidity">0</MinValue>
      <MaxValue field="humidity">100</MaxValue>
    </Validation>
  </Mapping>
</DataMappings>
```

#### 조건부 매핑 규칙
```xml
<ConditionalMappings>
  <Mapping name="AlarmData">
    <Condition>
      <Field>temperature</Field>
      <Operator>GreaterThan</Operator>
      <Value>80</Value>
    </Condition>
    <Action>
      <ApiEndpoint>AlarmEndpoint</ApiEndpoint>
      <Priority>High</Priority>
      <Template>AlarmTemplate</Template>
    </Action>
  </Mapping>
</ConditionalMappings>
```

### 2.4 로깅 정책 설정

#### Serilog 설정
```xml
<appSettings>
  <!-- Logging Configuration -->
  <add key="Serilog.MinimumLevel" value="Information" />
  <add key="Serilog.WriteTo.Console.Enabled" value="true" />
  <add key="Serilog.WriteTo.File.Enabled" value="true" />
  <add key="Serilog.WriteTo.File.Path" value="Logs/SimpleSerialToApi-.log" />
  <add key="Serilog.WriteTo.File.RollingInterval" value="Day" />
  <add key="Serilog.WriteTo.File.RetainedFileCountLimit" value="7" />
  <add key="Serilog.WriteTo.File.FileSizeLimitBytes" value="10485760" />
  
  <!-- EventLog (Windows Only) -->
  <add key="Serilog.WriteTo.EventLog.Enabled" value="true" />
  <add key="Serilog.WriteTo.EventLog.Source" value="SimpleSerialToApi" />
  <add key="Serilog.WriteTo.EventLog.LogName" value="Application" />
</appSettings>
```

#### 구조화된 로깅 템플릿
```xml
<LoggingTemplates>
  <Template name="SerialData">
    <Format>[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level:u3} Serial/{Port}: {Message}</Format>
    <Fields>
      <Field>Port</Field>
      <Field>DataLength</Field>
      <Field>Direction</Field>
    </Fields>
  </Template>
  <Template name="ApiCall">
    <Format>[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level:u3} API/{Endpoint}: {Message} ({Duration}ms)</Format>
    <Fields>
      <Field>Endpoint</Field>
      <Field>StatusCode</Field>
      <Field>Duration</Field>
    </Fields>
  </Template>
</LoggingTemplates>
```

### 2.5 보안 설정

#### 암호화 설정
```xml
<Security>
  <Encryption>
    <Provider>AES</Provider>
    <KeySize>256</KeySize>
    <Key><!-- Base64 encoded encryption key --></Key>
    <IV><!-- Base64 encoded initialization vector --></IV>
  </Encryption>
  <EncryptedSettings>
    <add name="API.AuthToken" />
    <add name="Database.ConnectionString" />
  </EncryptedSettings>
</Security>
```

#### 접근 제어 설정
```xml
<AccessControl>
  <Roles>
    <Role name="Administrator">
      <Permissions>
        <Permission>ViewSettings</Permission>
        <Permission>ModifySettings</Permission>
        <Permission>ViewLogs</Permission>
        <Permission>StartStop</Permission>
      </Permissions>
    </Role>
    <Role name="Operator">
      <Permissions>
        <Permission>ViewSettings</Permission>
        <Permission>ViewLogs</Permission>
        <Permission>StartStop</Permission>
      </Permissions>
    </Role>
  </Roles>
</AccessControl>
```

## 3. 모니터링 및 유지보수

### 3.1 시스템 모니터링

#### 성능 카운터 (Windows Performance Counters)
```csharp
// 모니터링 할 성능 지표
- Process\Private Bytes (메모리 사용량)
- Process\% Processor Time (CPU 사용률)
- .NET CLR Memory\# Bytes in all Heaps (관리 메모리)
- .NET CLR Exceptions\# of Exceps Thrown / sec (예외 발생률)
```

#### 커스텀 메트릭 수집
```xml
<Monitoring>
  <Metrics>
    <Counter name="SerialDataReceived" type="Rate" />
    <Counter name="ApiCallsSuccessful" type="Rate" />
    <Counter name="ApiCallsFailed" type="Rate" />
    <Counter name="QueueDepth" type="Gauge" />
    <Counter name="AverageProcessingTime" type="Timer" />
  </Metrics>
  <Alerts>
    <Alert metric="QueueDepth" threshold="900" action="EmailNotification" />
    <Alert metric="ApiCallsFailed" threshold="10" action="LogCritical" />
  </Alerts>
</Monitoring>
```

#### 헬스체크 엔드포인트
```xml
<HealthChecks>
  <Check name="SerialPort" timeout="5000" />
  <Check name="ApiEndpoint" timeout="10000" />
  <Check name="Database" timeout="5000" />
  <Check name="DiskSpace" minFreeGB="1" />
  <Check name="Memory" maxUsageMB="500" />
</HealthChecks>
```

### 3.2 로그 파일 관리

#### 로그 순환 정책
```xml
<LogManagement>
  <Rotation>
    <Interval>Daily</Interval>
    <MaxFiles>30</MaxFiles>
    <MaxSizeGB>1</MaxSizeGB>
    <CompressionAfterDays>7</CompressionAfterDays>
  </Rotation>
  <Archive>
    <Location>\\server\logs\archive</Location>
    <RetentionDays>365</RetentionDays>
    <CompressionLevel>Optimal</CompressionLevel>
  </Archive>
</LogManagement>
```

#### 로그 분석 쿼리 예제
```sql
-- SQL Server에서 로그 분석 (구조화된 로깅 사용 시)
SELECT 
  DATE(timestamp) as LogDate,
  COUNT(*) as TotalEvents,
  COUNT(CASE WHEN level = 'Error' THEN 1 END) as ErrorCount,
  COUNT(CASE WHEN level = 'Warning' THEN 1 END) as WarningCount
FROM ApplicationLogs
WHERE timestamp >= DATEADD(day, -7, GETDATE())
GROUP BY DATE(timestamp)
ORDER BY LogDate DESC;
```

### 3.3 성능 최적화

#### 메모리 튜닝
```xml
<Performance>
  <Memory>
    <GarbageCollection>
      <ServerGC>true</ServerGC>
      <ConcurrentGC>true</ConcurrentGC>
      <RetainVM>true</RetainVM>
    </GarbageCollection>
    <Limits>
      <MaxManagedMemoryMB>512</MaxManagedMemoryMB>
      <MaxNativeMemoryMB>256</MaxNativeMemoryMB>
    </Limits>
  </Memory>
  <Threading>
    <MaxWorkerThreads>20</MaxWorkerThreads>
    <MaxIOThreads>20</MaxIOThreads>
    <ThreadPoolMinThreads>5</ThreadPoolMinThreads>
  </Threading>
</Performance>
```

#### 큐 최적화
```xml
<MessageQueue>
  <Settings>
    <MaxCapacity>1000</MaxCapacity>
    <BatchSize>50</BatchSize>
    <BatchTimeout>5000</BatchTimeout>
    <ProcessingThreads>2</ProcessingThreads>
    <RetryQueue>
      <MaxCapacity>100</MaxCapacity>
      <RetryDelay>30000</RetryDelay>
    </RetryQueue>
  </Settings>
</MessageQueue>
```

### 3.4 백업 및 복구 절차

#### 자동 백업 설정
```xml
<Backup>
  <Schedule>
    <Daily time="02:00:00" />
    <Weekly day="Sunday" time="01:00:00" />
  </Schedule>
  <Items>
    <Item type="Configuration" path="App.config" />
    <Item type="Mappings" path="Mappings/*.xml" />
    <Item type="Logs" path="Logs/*.log" retention="30" />
    <Item type="Database" connection="LogDatabase" />
  </Items>
  <Destination>
    <Local path="C:\Backup\SimpleSerialToApi" />
    <Network path="\\backup-server\SimpleSerialToApi" />
    <Cloud provider="Azure" container="backup" />
  </Destination>
</Backup>
```

#### 재해복구 계획
```bash
# 1. 긴급 복구 (5분 내)
- 애플리케이션 재시작
- 기본 설정으로 복원
- Serial 포트 연결 확인

# 2. 부분 복구 (30분 내)
- 백업에서 설정 파일 복원
- 데이터베이스 연결 복구
- 로그 시스템 복구

# 3. 전체 복구 (2시간 내)
- 전체 시스템 재설치
- 백업 데이터 완전 복원
- 모든 기능 검증
```

## 4. 장애 대응 절차

### 4.1 일반적인 장애 패턴

#### Serial 통신 장애
```
증상: COM 포트 연결 실패
진단: Event Viewer, Device Manager 확인
복구: 
  1. Serial 케이블 및 장비 상태 확인
  2. 드라이버 재설치
  3. 포트 설정 재구성
  4. 애플리케이션 재시작
```

#### API 통신 장애
```
증상: HTTP 오류 응답
진단: Network Monitor, API 로그 확인
복구:
  1. 네트워크 연결 상태 확인
  2. API 인증 토큰 갱신
  3. 재시도 정책 확인
  4. 백업 API 엔드포인트로 전환
```

#### 메모리 부족
```
증상: OutOfMemoryException
진단: Performance Monitor, 메모리 덤프 분석
복구:
  1. 애플리케이션 재시작
  2. 로그 레벨 조정
  3. 큐 크기 축소
  4. GC 튜닝 적용
```

### 4.2 장애 대응 프로세스

#### 1단계: 장애 감지
- 자동 알림 시스템
- 모니터링 도구 확인
- 사용자 신고 접수

#### 2단계: 초기 대응 (5분 내)
```powershell
# 긴급 상태 확인 스크립트
$processName = "SimpleSerialToApi"
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

if (-not $process) {
    Write-Host "Process not running. Attempting restart..."
    Start-Process -FilePath "C:\Program Files\SimpleSerialToApi\SimpleSerialToApi.exe"
} else {
    Write-Host "Process running. Memory usage: $($process.WorkingSet64 / 1MB) MB"
}

# Serial 포트 상태 확인
Get-WmiObject -Class Win32_SerialPort | Select-Object Name, Status, DeviceID
```

#### 3단계: 상세 진단 (15분 내)
- 로그 파일 분석
- 성능 카운터 확인
- 시스템 리소스 점검
- 네트워크 연결 테스트

#### 4단계: 복구 작업 (30분 내)
- 설정 파일 복원
- 서비스 재시작
- 데이터베이스 연결 복구
- API 엔드포인트 테스트

#### 5단계: 사후 검토
- 장애 원인 분석
- 재발 방지 대책 수립
- 모니터링 개선
- 문서 업데이트

## 5. 운영 체크리스트

### 5.1 일일 점검 항목
- [ ] 애플리케이션 실행 상태 확인
- [ ] Serial 포트 연결 상태 점검
- [ ] API 호출 성공률 확인
- [ ] 오류 로그 검토
- [ ] 큐 깊이 및 처리량 확인
- [ ] 시스템 리소스 사용률 점검

### 5.2 주간 점검 항목
- [ ] 로그 파일 크기 및 순환 상태 확인
- [ ] 백업 작업 성공 여부 검증
- [ ] 성능 지표 추이 분석
- [ ] 보안 업데이트 확인
- [ ] 설정 변경 사항 문서화

### 5.3 월간 점검 항목
- [ ] 전체 시스템 성능 리뷰
- [ ] 용량 계획 및 확장성 검토
- [ ] 재해복구 절차 테스트
- [ ] 보안 감사 수행
- [ ] 사용자 피드백 수집 및 반영

## 6. 고급 구성 예제

### 6.1 로드 밸런싱 설정
```xml
<LoadBalancing>
  <Strategy>RoundRobin</Strategy>
  <Endpoints>
    <Endpoint url="https://api1.company.com" weight="3" />
    <Endpoint url="https://api2.company.com" weight="2" />
    <Endpoint url="https://api3.company.com" weight="1" />
  </Endpoints>
  <HealthCheck interval="60000" timeout="5000" />
</LoadBalancing>
```

### 6.2 데이터 압축 설정
```xml
<Compression>
  <Enabled>true</Enabled>
  <Algorithm>GZip</Algorithm>
  <MinSizeBytes>1024</MinSizeBytes>
  <CompressionLevel>Optimal</CompressionLevel>
</Compression>
```

### 6.3 캐싱 전략
```xml
<Caching>
  <ApiResponses>
    <TTL>300000</TTL>
    <MaxEntries>1000</MaxEntries>
    <EvictionPolicy>LRU</EvictionPolicy>
  </ApiResponses>
  <ConfigurationData>
    <TTL>3600000</TTL>
    <RefreshOnAccess>true</RefreshOnAccess>
  </ConfigurationData>
</Caching>
```

이 관리자 매뉴얼을 통해 SimpleSerialToApi의 모든 고급 기능을 효과적으로 관리하고 최적의 성능을 유지할 수 있습니다.