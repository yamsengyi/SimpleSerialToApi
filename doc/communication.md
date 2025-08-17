# Communication Enhancement Implementation Plan

## 개요
SimpleSerialToApi 애플리케이션에 고급 통신 기능과 모니터링 기능을 추가하는 계획입니다.

## 구현 기능 목록

### 1. 장비 식별 시스템 (Device Identification)

#### 1.1 API 서버 식별값 관리
- **목적**: API 서버로 데이터 전송 시 장비를 식별할 수 있는 고유값 설정
- **구현 위치**: 
  - UI: MainWindow.xaml에 Device ID 입력 섹션 추가
  - 설정: App.config에 DeviceId 항목 추가
  - 로직: MainViewModel에 DeviceId 속성 및 설정 명령 추가

#### 1.2 구현 세부사항
```xml
<!-- App.config 추가 항목 -->
<add key="DeviceId" value="" />
```

```csharp
// MainViewModel 추가 속성
public string DeviceId { get; set; }
public ICommand SetDeviceIdCommand { get; }
```

### 2. 데이터 매핑 시스템 (Data Mapping System)

#### 2.1 시나리오 기반 데이터 처리
- **최대 시나리오**: 10개
- **매핑 대상**: 시리얼 데이터 및 API Response 데이터
- **식별 방식**: indexOf를 사용한 문자열 매칭

#### 2.2 매핑 구성 요소
1. **데이터 소스 위치**: 
   - Serial Communication
   - API Response (JSON)

2. **시나리오 식별자**: 
   - 입력된 데이터에서 시나리오를 구분하는 문자열
   - 예: "TEMP", "HUMID", "ERROR" 등

3. **전송값 지정**: 
   - 식별된 시나리오에 따른 전송할 데이터 포맷
   - 예약어 지원

4. **전송 방식**: 
   - Serial Communication
   - API 호출 (GET, POST, PUT, DELETE 등)

#### 2.3 예약어 시스템
모든 예약어는 `@` 기호로 시작:

| 예약어 | 설명 | 예시 |
|--------|------|------|
| @yyyyMMddHHmmssfff | 밀리초 포함 날짜시간 | 20250814143052123 |
| @yyyyMMddHHmmss | 초단위 날짜시간 | 20250814143052 |
| @yyyyMMdd | 날짜만 | 20250814 |
| @deviceId | 설정된 장비 ID | DEVICE001 |

#### 2.4 UI 구성
- **DataGrid 또는 다중 TextBox**: 최대 10개 시나리오 설정
- **컬럼 구성**:
  1. 활성화 (Checkbox)
  2. 시나리오 이름
  3. 데이터 소스 (Serial/API Response)
  4. 식별자 문자열
  5. 전송값 템플릿
  6. 전송 방식 (Serial/API)
  7. API Method (GET/POST/PUT/DELETE)
  8. 테스트 버튼

### 3. 시리얼 통신 모니터 (Serial Communication Monitor)

#### 3.1 기능 요구사항
- **실시간 모니터링**: 송수신 데이터를 실시간으로 표시
- **데이터 표시 형식**: 
  - 타임스탬프
  - 방향 (송신/수신)
  - 원본 데이터
  - 해석된 데이터 (매핑 시나리오 적용 결과)

#### 3.2 UI 구성
- **터미널 형태**: ScrollViewer + TextBox 또는 RichTextBox
- **컨트롤**: 
  - Clear 버튼
  - Save to File 버튼
  - Show/Hide 토글 버튼
  - 자동 스크롤 Checkbox

#### 3.3 표시 예시
```
[2025-08-14 14:30:52.123] [RX] STX TEMP:25.6 ETX
[2025-08-14 14:30:52.125] [MAPPED] Scenario: Temperature Reading → API POST
[2025-08-14 14:30:52.130] [TX] Response ACK
```

### 4. API 통신 모니터 (API Communication Monitor)

#### 4.1 기능 요구사항
- **요청/응답 추적**: 모든 API 호출과 응답을 기록
- **상세 정보 표시**:
  - 타임스탬프
  - HTTP Method
  - URL
  - Request Headers/Body
  - Response Status Code
  - Response Headers/Body
  - 응답 시간

#### 4.2 UI 구성
- **터미널 형태**: 시리얼 모니터와 동일한 구조
- **필터링 기능**: 
  - Status Code별 필터
  - Method별 필터
  - 시간 범위 필터

#### 4.3 표시 예시
```
[2025-08-14 14:30:52.200] [REQ] POST https://api.example.com/data
[2025-08-14 14:30:52.205] [BODY] {"deviceId":"@deviceId","temp":25.6,"timestamp":"@yyyyMMddHHmmss"}
[2025-08-14 14:30:52.450] [RES] 200 OK (250ms)
[2025-08-14 14:30:52.451] [BODY] {"status":"success","id":12345}
```

### 5. 터미널 UI 통합

#### 5.1 레이아웃 구성
```
┌─────────────────────────────────────────────────────────┐
│ Main Application UI                                     │
├─────────────────────────────────────────────────────────┤
│ [Show Serial Monitor] [Show API Monitor]                │
├─────────────────────────────────────────────────────────┤
│ Serial Communication Monitor                            │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ [Clear] [Save] [Auto Scroll ☑]              [Hide] │ │
│ │ ───────────────────────────────────────────────────│ │
│ │ Terminal Output Area                                │ │
│ │                                                     │ │
│ └─────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│ API Communication Monitor                               │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ [Clear] [Save] [Filter ▼]                   [Hide] │ │
│ │ ───────────────────────────────────────────────────│ │
│ │ Terminal Output Area                                │ │
│ │                                                     │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## 구현 단계별 계획

### Phase 1: 기본 구조 설정
1. **Models 생성**:
   - `DataMappingScenario.cs`: 매핑 시나리오 모델
   - `MonitorMessage.cs`: 모니터 메시지 모델
   - `CommunicationEvent.cs`: 통신 이벤트 모델

2. **Services 확장**:
   - `DataMappingService.cs`: 시나리오 기반 데이터 매핑
   - `SerialMonitorService.cs`: 시리얼 통신 모니터링
   - `ApiMonitorService.cs`: API 통신 모니터링
   - `ReservedWordService.cs`: 예약어 처리

### Phase 2: UI 구성
1. **MainWindow.xaml 확장**:
   - Device ID 설정 섹션
   - Data Mapping Grid
   - Monitor 토글 버튼

2. **새로운 UserControl 생성**:
   - `DataMappingControl.xaml`: 매핑 설정 UI
   - `TerminalMonitorControl.xaml`: 터미널 모니터 UI

### Phase 3: 비즈니스 로직 구현
1. **DataMappingService**: 
   - 시나리오 매칭 로직
   - 예약어 치환 로직
   - 동적 전송 처리

2. **Monitor Services**:
   - 실시간 데이터 수집
   - 이벤트 기반 알림
   - 데이터 저장/내보내기

### Phase 4: 통합 및 테스트
1. **기존 서비스 통합**:
   - SerialCommunicationService와 모니터 연동
   - HttpApiClientService와 모니터 연동
   - TimerService와 매핑 시스템 연동

2. **UI 바인딩 및 테스트**:
   - ViewModel 확장
   - Command 구현
   - 실제 데이터 흐름 테스트

## 설정 파일 확장

### App.config 추가 항목
```xml
<!-- Device Identification -->
<add key="DeviceId" value="" />

<!-- Data Mapping Settings -->
<add key="EnableDataMapping" value="true" />
<add key="MaxMappingScenarios" value="10" />

<!-- Monitor Settings -->
<add key="SerialMonitorEnabled" value="true" />
<add key="ApiMonitorEnabled" value="true" />
<add key="MonitorMaxLines" value="1000" />
<add key="MonitorAutoScroll" value="true" />
```

## 주요 클래스 구조

### DataMappingScenario
```csharp
public class DataMappingScenario
{
    public bool IsEnabled { get; set; }
    public string Name { get; set; }
    public DataSource Source { get; set; }  // Serial, ApiResponse
    public string Identifier { get; set; }
    public string ValueTemplate { get; set; }
    public TransmissionType TransmissionType { get; set; }  // Serial, Api
    public HttpMethod ApiMethod { get; set; }
    public string ApiEndpoint { get; set; }
}
```

### MonitorMessage
```csharp
public class MonitorMessage
{
    public DateTime Timestamp { get; set; }
    public MessageDirection Direction { get; set; }  // Send, Receive
    public MessageType Type { get; set; }  // Serial, Api, Mapped
    public string Content { get; set; }
    public string AdditionalInfo { get; set; }
}
```

## 성능 고려사항

1. **메모리 관리**: 모니터 메시지는 최대 개수 제한 설정
2. **UI 응답성**: 백그라운드 스레드에서 데이터 처리 후 UI 스레드로 마샬링
3. **파일 I/O**: 대용량 로그 저장 시 비동기 처리
4. **정규표현식**: 식별자 매칭 시 성능 최적화 고려

## 보안 고려사항

1. **민감 데이터**: API 인증 정보는 별도 암호화 저장
2. **로그 데이터**: 개인정보 포함 가능성 있는 데이터 마스킹
3. **네트워크 통신**: HTTPS 사용 권장
4. **설정 파일**: 중요 설정은 사용자별 보호된 위치에 저장

이 문서는 구현 과정에서 지속적으로 업데이트됩니다.
