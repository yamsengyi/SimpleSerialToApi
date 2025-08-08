# SimpleSerialToApi - API 연동 가이드

## 1. 개요

SimpleSerialToApi는 다양한 REST API와 연동하여 Serial 장비 데이터를 전송할 수 있습니다. 이 문서는 API 연동을 위한 상세 가이드를 제공합니다.

## 2. 지원되는 API 형식

### 2.1 REST API 기본 요구사항
- **Protocol**: HTTP/HTTPS
- **Methods**: POST, PUT, PATCH
- **Content-Type**: application/json
- **Response Format**: JSON (권장)

### 2.2 지원되는 인증 방식

#### Bearer Token Authentication
```http
POST /api/sensors/data HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "deviceId": "TEMP_001",
  "timestamp": "2025-01-15T10:30:00Z",
  "data": {...}
}
```

#### Basic Authentication
```http
POST /api/sensors/data HTTP/1.1
Host: api.example.com
Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=
Content-Type: application/json
```

#### API Key Authentication
```http
POST /api/sensors/data HTTP/1.1
Host: api.example.com
X-API-Key: your-api-key-here
Content-Type: application/json
```

#### OAuth 2.0 (Client Credentials)
```http
POST /oauth/token HTTP/1.1
Host: auth.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=your_client_id&client_secret=your_secret
```

### 2.3 커스텀 헤더 지원
```xml
<CustomHeaders>
  <Header name="X-Client-Version" value="1.0.0" />
  <Header name="X-Device-Type" value="SerialSensor" />
  <Header name="X-Correlation-ID" value="{guid}" />
</CustomHeaders>
```

## 3. 데이터 형식 및 예제

### 3.1 기본 데이터 구조
```json
{
  "deviceId": "string",           // 장비 고유 식별자
  "timestamp": "ISO8601",         // 데이터 수집 시간
  "location": "string",           // 장비 위치 (선택사항)
  "data": {                       // 실제 센서 데이터
    // 장비별 데이터 구조
  },
  "metadata": {                   // 메타데이터 (선택사항)
    "version": "1.0",
    "quality": "good",
    "source": "SimpleSerialToApi"
  }
}
```

### 3.2 온도 센서 데이터 예제
```json
{
  "deviceId": "TEMP_001",
  "timestamp": "2025-01-15T10:30:00Z",
  "location": "Building A - Room 101",
  "data": {
    "temperature": 25.5,
    "humidity": 60.0,
    "unit": "celsius"
  },
  "metadata": {
    "version": "1.0",
    "quality": "good",
    "source": "SimpleSerialToApi",
    "serialPort": "COM1"
  }
}
```

### 3.3 압력 센서 데이터 예제
```json
{
  "deviceId": "PRESS_002",
  "timestamp": "2025-01-15T10:30:15Z",
  "location": "Pipeline Section A",
  "data": {
    "pressure": 1013.25,
    "unit": "hPa",
    "status": "normal"
  },
  "metadata": {
    "version": "1.0",
    "quality": "good",
    "source": "SimpleSerialToApi",
    "serialPort": "COM2"
  }
}
```

### 3.4 배치 데이터 전송
```json
{
  "batchId": "batch_20250115_103000",
  "timestamp": "2025-01-15T10:30:00Z",
  "count": 3,
  "data": [
    {
      "deviceId": "TEMP_001",
      "timestamp": "2025-01-15T10:29:45Z",
      "data": {"temperature": 25.5, "humidity": 60.0}
    },
    {
      "deviceId": "TEMP_001", 
      "timestamp": "2025-01-15T10:29:50Z",
      "data": {"temperature": 25.6, "humidity": 59.8}
    },
    {
      "deviceId": "TEMP_001",
      "timestamp": "2025-01-15T10:29:55Z", 
      "data": {"temperature": 25.4, "humidity": 60.2}
    }
  ]
}
```

## 4. API 응답 처리

### 4.1 성공 응답
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "message": "Data received successfully",
  "id": "data_12345",
  "timestamp": "2025-01-15T10:30:01Z"
}
```

```json
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/sensors/data/12345

{
  "id": "12345",
  "status": "created",
  "timestamp": "2025-01-15T10:30:01Z"
}
```

### 4.2 오류 응답 처리

#### 클라이언트 오류 (4xx)
```json
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": "invalid_request",
  "message": "Missing required field: deviceId",
  "details": [
    {
      "field": "deviceId",
      "error": "required_field_missing"
    }
  ]
}
```

```json
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "error": "unauthorized",
  "message": "Invalid or expired token"
}
```

```json
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/json

{
  "error": "validation_failed",
  "message": "Data validation failed",
  "details": [
    {
      "field": "temperature",
      "error": "value_out_of_range",
      "message": "Temperature must be between -50 and 100"
    }
  ]
}
```

#### 서버 오류 (5xx)
```json
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "error": "internal_error",
  "message": "Server encountered an unexpected condition",
  "requestId": "req_12345"
}
```

```json
HTTP/1.1 503 Service Unavailable
Content-Type: application/json
Retry-After: 300

{
  "error": "service_unavailable",
  "message": "Service temporarily unavailable",
  "retryAfter": 300
}
```

### 4.3 재시도 정책

SimpleSerialToApi는 다음과 같은 재시도 정책을 적용합니다:

#### 재시도 대상 상태 코드
- `408` Request Timeout
- `429` Too Many Requests
- `500` Internal Server Error  
- `502` Bad Gateway
- `503` Service Unavailable
- `504` Gateway Timeout

#### 재시도 전략
```csharp
// 지수 백오프 전략
Retry Count: 1 → Delay: 1초
Retry Count: 2 → Delay: 2초
Retry Count: 3 → Delay: 4초
Retry Count: 4 → Delay: 8초 (최대 재시도)
```

#### 재시도 불가 상태 코드
- `400` Bad Request
- `401` Unauthorized
- `403` Forbidden
- `404` Not Found
- `422` Unprocessable Entity

## 5. API 설정 구성

### 5.1 App.config 설정
```xml
<appSettings>
  <!-- API 기본 설정 -->
  <add key="API.BaseUrl" value="https://api.example.com" />
  <add key="API.Endpoint" value="/sensors/data" />
  <add key="API.Method" value="POST" />
  <add key="API.ContentType" value="application/json" />
  <add key="API.Timeout" value="30000" />
  
  <!-- 인증 설정 -->
  <add key="API.AuthType" value="Bearer" />
  <add key="API.AuthToken" value="your-token-here" />
  
  <!-- 배치 처리 설정 -->
  <add key="API.BatchEnabled" value="true" />
  <add key="API.BatchSize" value="50" />
  <add key="API.BatchTimeout" value="5000" />
  
  <!-- 재시도 정책 -->
  <add key="API.Retry.MaxAttempts" value="3" />
  <add key="API.Retry.InitialDelay" value="1000" />
  <add key="API.Retry.MaxDelay" value="30000" />
</appSettings>
```

### 5.2 JSON 설정 (appsettings.json)
```json
{
  "ApiConfiguration": {
    "BaseUrl": "https://api.example.com",
    "Endpoints": {
      "SensorData": "/sensors/data",
      "DeviceStatus": "/devices/status",
      "Alerts": "/alerts"
    },
    "Authentication": {
      "Type": "Bearer",
      "Token": "your-token-here",
      "RefreshUrl": "/auth/refresh",
      "RefreshInterval": 3600
    },
    "BatchSettings": {
      "Enabled": true,
      "MaxSize": 50,
      "TimeoutMs": 5000
    },
    "RetryPolicy": {
      "MaxAttempts": 3,
      "InitialDelayMs": 1000,
      "MaxDelayMs": 30000,
      "BackoffMultiplier": 2.0
    }
  }
}
```

## 6. 고급 API 연동

### 6.1 다중 엔드포인트 지원
```xml
<ApiEndpoints>
  <Endpoint name="Primary">
    <Url>https://api-primary.com/data</Url>
    <Priority>1</Priority>
    <Enabled>true</Enabled>
    <HealthCheckUrl>https://api-primary.com/health</HealthCheckUrl>
  </Endpoint>
  <Endpoint name="Secondary">
    <Url>https://api-backup.com/data</Url>
    <Priority>2</Priority>
    <Enabled>true</Enabled>
    <HealthCheckUrl>https://api-backup.com/health</HealthCheckUrl>
  </Endpoint>
</ApiEndpoints>
```

### 6.2 조건부 API 호출
```xml
<ConditionalRouting>
  <Route condition="temperature > 80">
    <Endpoint>AlertEndpoint</Endpoint>
    <Template>HighTemperatureAlert</Template>
    <Priority>High</Priority>
  </Route>
  <Route condition="pressure < 900">
    <Endpoint>AlertEndpoint</Endpoint>
    <Template>LowPressureAlert</Template>
    <Priority>Critical</Priority>
  </Route>
</ConditionalRouting>
```

### 6.3 데이터 변환 규칙
```xml
<DataTransformation>
  <Rule name="TemperatureConversion">
    <Input field="temperature" />
    <Output field="temp_celsius" />
    <Transform>fahrenheit_to_celsius</Transform>
  </Rule>
  <Rule name="TimestampFormat">
    <Input field="timestamp" />
    <Output field="timestamp" />
    <Transform>iso8601_utc</Transform>
  </Rule>
</DataTransformation>
```

## 7. API 테스팅 및 검증

### 7.1 연결 테스트
```bash
# cURL을 이용한 기본 연결 테스트
curl -X POST https://api.example.com/sensors/data \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "TEST_001",
    "timestamp": "2025-01-15T10:30:00Z",
    "data": {
      "temperature": 25.0
    }
  }'
```

### 7.2 PowerShell 테스트 스크립트
```powershell
# API 연결 테스트 스크립트
$headers = @{
    'Authorization' = 'Bearer your-token'
    'Content-Type' = 'application/json'
}

$body = @{
    deviceId = "TEST_001"
    timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    data = @{
        temperature = 25.0
        humidity = 60.0
    }
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'https://api.example.com/sensors/data' -Method POST -Headers $headers -Body $body
    Write-Host "Success: $($response.message)" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
```

### 7.3 애플리케이션 내 테스트 기능
SimpleSerialToApi는 다음과 같은 내장 테스트 기능을 제공합니다:

- **Connection Test**: API 엔드포인트 연결 가능 여부 확인
- **Authentication Test**: 인증 토큰 유효성 검증
- **Data Format Test**: 샘플 데이터 전송 테스트
- **Performance Test**: 응답시간 및 처리량 측정

## 8. 보안 고려사항

### 8.1 전송 보안
- **HTTPS 사용 필수**: 모든 API 통신은 HTTPS로 암호화
- **TLS 1.2 이상**: 최신 TLS 버전 사용
- **Certificate Validation**: SSL 인증서 유효성 검증

### 8.2 인증 보안  
- **Token Rotation**: 정기적인 토큰 갱신
- **Secure Storage**: 인증 정보의 안전한 저장
- **Audit Logging**: 모든 API 호출 기록

### 8.3 데이터 보안
- **데이터 최소화**: 필요한 데이터만 전송
- **민감정보 마스킹**: 로그에서 민감정보 제거
- **암호화**: 필요시 추가 데이터 암호화

## 9. 성능 최적화

### 9.1 배치 처리
```xml
<BatchConfiguration>
  <MaxBatchSize>100</MaxBatchSize>
  <BatchTimeoutMs>5000</BatchTimeoutMs>
  <MaxConcurrentBatches>3</MaxConcurrentBatches>
</BatchConfiguration>
```

### 9.2 연결 관리
```xml
<ConnectionPooling>
  <MaxConnections>10</MaxConnections>
  <ConnectionTimeout>30000</ConnectionTimeout>
  <KeepAlive>true</KeepAlive>
  <MaxIdleTime>300000</MaxIdleTime>
</ConnectionPooling>
```

### 9.3 압축 사용
```xml
<Compression>
  <Enabled>true</Enabled>
  <Algorithm>gzip</Algorithm>
  <MinSize>1024</MinSize>
</Compression>
```

## 10. 모니터링 및 로깅

### 10.1 API 호출 로깅
```csharp
// 구조화된 로그 예제
Log.Information("API call initiated", new {
    Endpoint = "https://api.example.com/sensors/data",
    Method = "POST", 
    DeviceId = "TEMP_001",
    CorrelationId = correlationId
});

Log.Information("API call completed", new {
    Endpoint = "https://api.example.com/sensors/data",
    StatusCode = 200,
    Duration = duration.TotalMilliseconds,
    CorrelationId = correlationId
});
```

### 10.2 메트릭 수집
- **Success Rate**: API 호출 성공률
- **Response Time**: 평균 응답 시간
- **Throughput**: 초당 처리 건수
- **Error Rate**: 오류 발생률

### 10.3 알림 설정
```xml
<Alerting>
  <Alert name="HighErrorRate">
    <Condition>error_rate > 5%</Condition>
    <Action>email</Action>
    <Recipients>admin@company.com</Recipients>
  </Alert>
  <Alert name="SlowResponse">
    <Condition>avg_response_time > 5000</Condition>
    <Action>log</Action>
    <Severity>warning</Severity>
  </Alert>
</Alerting>
```

## 11. 문제 해결

### 11.1 일반적인 문제들

#### 연결 타임아웃
```
문제: API 호출이 타임아웃됨
원인: 네트워크 지연 또는 서버 응답 지연
해결: Timeout 설정 증가, 네트워크 상태 확인
```

#### 인증 실패
```
문제: 401 Unauthorized 응답
원인: 토큰 만료 또는 잘못된 인증 정보
해결: 토큰 갱신, 인증 설정 확인
```

#### 데이터 형식 오류
```
문제: 422 Unprocessable Entity 응답
원인: API 서버가 기대하는 데이터 형식과 불일치
해결: API 명세 확인, 데이터 매핑 규칙 수정
```

### 11.2 디버깅 도구
- **Fiddler**: HTTP 트래픽 모니터링
- **Postman**: API 테스트 도구
- **curl**: 명령줄 HTTP 클라이언트
- **내장 로깅**: 상세 API 호출 로그

## 12. API 명세 예제

### 12.1 OpenAPI 3.0 명세
```yaml
openapi: 3.0.0
info:
  title: Sensor Data API
  version: 1.0.0
paths:
  /sensors/data:
    post:
      summary: Send sensor data
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/SensorData'
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SuccessResponse'
        '400':
          description: Bad Request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
components:
  schemas:
    SensorData:
      type: object
      required:
        - deviceId
        - timestamp
        - data
      properties:
        deviceId:
          type: string
        timestamp:
          type: string
          format: date-time
        data:
          type: object
```

이 가이드를 참조하여 SimpleSerialToApi를 다양한 REST API와 안정적으로 연동할 수 있습니다.