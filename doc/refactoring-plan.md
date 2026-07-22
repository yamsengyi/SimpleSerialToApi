# MainViewModel 리팩토링 계획

> **목표**: 2,130줄의 `MainViewModel`을 단일 책임 원칙(SRP)에 따라 7개의 파사드(Facade)로 분리하여 유지보수성과 테스트 용이성을 향상시킨다.

---

## 현재 상태 분석

### MainViewModel 현재 구조

| 항목 | 값 |
|------|-----|
| 총 라인 수 | ~2,130줄 |
| Inject된 서비스 | 13개 |
| ICommand 속성 | 26개 |
| Public 속성 | 30+개 |
| Private 메서드 | 30+개 |

### 현재 의존성 그래프

```
MainViewModel
├── ILogger<MainViewModel>
├── SerialCommunicationService
├── SimpleQueueService
├── SimpleHttpService
├── ComPortDiscoveryService
├── DataMappingService
├── SerialMonitorService
├── ApiMonitorService
├── ReservedWordService
├── SerialDataSimulator
├── IConfigurationService
├── IQueueManager
└── IQueueProcessor<MappedApiData>
```

MainViewModel이 13개의 서비스를 직접 주입받아 모든 책임을 떠안고 있음.

---

## 대상 파사드 목록

### 1️⃣ SerialConnectionFacade — 시리얼 연결/포트 관리

**책임**: 시리얼 포트 연결, 해제, 포트 검색, 스마트 선택, 자동 연결

| 메서드 | 설명 |
|--------|------|
| `Connect()` | 시리얼 포트 연결 |
| `Disconnect()` | 시리얼 포트 해제 |
| `RefreshPorts()` | 포트 목록 갱신 |
| `PerformSmartSelection()` | 최적 포트 자동 선택 |
| `InitializeSmartPortSelection()` | 초기 스마트 선택 |
| `CheckAutoConnect()` | 자동 연결 확인 |
| `CanConnect()` / `CanDisconnect()` | 연결 가능 여부 반환 |

**이관될 속성**: `SerialPort`, `IsConnected`, `AvailablePorts`, `SerialConnectionStatus`

**의존성**: `SerialCommunicationService`, `ComPortDiscoveryService`

---

### 2️⃣ ConfigurationFacade — 앱 설정 관리

**책임**: API URL, Queue 설정, Device ID, 시리얼 설정 로드/저장

| 메서드 | 설명 |
|--------|------|
| `LoadApiUrl()` | API URL 로드 |
| `LoadQueueSettings()` | Queue 설정 로드 |
| `SetTransmissionInterval()` | 전송 간격 저장 |
| `SetBatchSize()` | 배치 크기 저장 |
| `SetDeviceId()` | Device ID 저장 |
| `OpenSerialConfig()` | 시리얼 설정 창 열기 |

**이관될 속성**: `TransmissionInterval`, `BatchSize`, `DeviceId`, `ApiUrl`

**의존성**: `IConfigurationService`, `SerialCommunicationService`

---

### 3️⃣ DataMappingFacade — 데이터 매핑 시나리오 관리

**책임**: 매핑 시나리오 CRUD, 저장, 테스트

| 메서드 | 설명 |
|--------|------|
| `AddMappingScenario()` | 새 시나리오 추가 |
| `DeleteMappingScenario()` | 선택 시나리오 삭제 |
| `TestMapping()` | 매핑 테스트 실행 |
| `SaveMapping()` | 시나리오 파일 저장 |
| `ApplyDataMapping()` | 적용 후 창 닫기 |
| `CancelDataMapping()` | 취소 후 창 닫기 |
| `InitializeMappingScenarios()` | 서비스에서 시나리오 로드 |
| `GetApiEndpointForScenario()` | 시나리오별 API 엔드포인트 조합 |

**이관될 속성**: `MappingScenarios`, `SelectedMappingScenario`, `DataSources`, `TransmissionTypes`, `ApiMethods`, `ContentTypes`, `MappingScenariosCount`

**의존성**: `DataMappingService`

---

### 4️⃣ DataTransmissionFacade — 데이터 전송 처리

**책임**: 시리얼 데이터 수신 처리, API/시리얼 전송, 큐 처리

| 메서드 | 설명 |
|--------|------|
| `OnSerialDataReceived()` | 시리얼 데이터 수신 이벤트 처리 |
| `OnMappingProcessed()` | 매핑 완료 이벤트 처리 |
| `ProcessApiTransmission()` | API 전송 (큐 방식) |
| `ProcessApiTransmissionFallback()` | API 전송 폴백 (직접 전송) |
| `ProcessSerialTransmission()` | 시리얼 포트로 전송 |
| `OnSimulatedDataReceived()` | 시뮬레이션 데이터 수신 처리 |
| `InitializeQueueProcessing()` | 큐 매니저 초기화 및 처리 시작 |
| `UpdateQueueCount()` | 큐 카운트 갱신 |
| `TestApi()` | API 연결 테스트 |

**이관될 속성**: `QueueCount`, `ApiUrl` (테스트 전용)

**의존성**: `SerialCommunicationService`, `SimpleQueueService`, `SimpleHttpService`, `DataMappingService`, `IQueueManager`, `IQueueProcessor<MappedApiData>`, `ApiMonitorService`, `SerialMonitorService`

> ⚠️ **가장 복잡한 파사드**. 내부에서 다시 하위 헬퍼로 분할 가능.

---

### 5️⃣ MonitorFacade — 시리얼/API 모니터 관리

**책임**: 모니터 메시지 표시, 저장, 초기화, 필터링

| 메서드 | 설명 |
|--------|------|
| `ShowSerialMonitor()` / `HideSerialMonitor()` | 시리얼 모니터 표시/숨김 |
| `ToggleSerialMonitor()` | 시리얼 모니터 토글 |
| `ShowApiMonitor()` / `HideApiMonitor()` | API 모니터 표시/숨김 |
| `ToggleApiMonitor()` | API 모니터 토글 |
| `SaveSerialMonitor()` | 시리얼 모니터 로그 저장 |
| `SaveApiMonitor()` | API 모니터 로그 저장 |
| `ClearSerialMonitor()` | 시리얼 모니터 초기화 |
| `ClearApiMonitor()` | API 모니터 초기화 |
| `LoadExistingSerialMessages()` | 기존 시리얼 메시지 로드 |
| `LoadExistingApiMessages()` | 기존 API 메시지 로드 |
| `OnSerialMonitorMessageAdded()` | 시리얼 메시지 추가 이벤트 |
| `OnApiMonitorMessageAdded()` | API 메시지 추가 이벤트 |

**이관될 속성**: `SerialMonitorText`, `ApiMonitorText`, `SerialMonitorVisible`, `ApiMonitorVisible`, `SerialMonitorAutoScroll`, `ApiMonitorAutoScroll`, `SerialMonitorStatus`, `ApiMonitorStatus`, `SerialMonitorButtonText`, `ApiMonitorButtonText`, `SerialMonitorFilter`, `ApiMonitorFilter`, `SerialMonitorFilters`, `ApiMonitorFilters`, `SerialShowTimestamps`, `ApiShowHeaders`, `SerialMessageCount`, `ApiRequestCount`, `ApiSuccessRate`

**의존성**: `SerialMonitorService`, `ApiMonitorService`

---

### 6️⃣ WindowManagementFacade — 팝업 창 관리

**책임**: 모든 자식 창(DataMapping, ReservedWords, SerialMonitor, ApiMonitor)의 생성/포커스/추적/종료

| 메서드 | 설명 |
|--------|------|
| `OpenDataMapping()` | 데이터 매핑 창 열기 또는 포커스 |
| `ShowReservedWords()` | 예약어 창 열기 또는 포커스 |
| `OpenSerialMonitor()` | 시리얼 모니터 창 열기 또는 포커스 |
| `OpenApiMonitor()` | API 모니터 창 열기 또는 포커스 |
| `CloseAllChildWindows()` | 모든 열린 창 닫기 |

**이관될 필드**: `_dataMappingWindow`, `_reservedWordsWindow`, `_serialMonitorWindow`, `_apiMonitorWindow`

**의존성**: `DataMappingService` (리로드 용도, 선택적)

---

### 7️⃣ SimulationFacade — 시뮬레이션 관리

**책임**: 테스트 데이터 생성 시뮬레이션 제어

| 메서드 | 설명 |
|--------|------|
| `StartSimulation()` | 시뮬레이션 시작 |
| `StopSimulation()` | 시뮬레이션 중지 |
| `ToggleSimulation()` | 시뮬레이션 토글 |
| `GenerateSingleData()` | 단일 시뮬레이션 데이터 생성 |

**이관될 속성**: `IsSimulating`, `SimulationInterval`, `SimulationButtonText`

**의존성**: `SerialDataSimulator`

---

## 목표 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                     MainViewModel (Facade)                    │
│  Inject: 7 interfaces (↓13 → ↓7), 남은 Properties/Commands   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐  ┌─────────────────┐                    │
│  │ SerialConnection│  │ Configuration   │  ...                │
│  │ Facade          │  │ Facade          │                    │
│  └────────┬────────┘  └────────┬────────┘                    │
│           │                    │                              │
│  ┌────────▼────────┐  ┌────────▼────────┐                    │
│  │ DataMapping     │  │ DataTransmission│                    │
│  │ Facade          │  │ Facade          │                    │
│  └────────┬────────┘  └────────┬────────┘                    │
│           │                    │                              │
│  ┌────────▼────────┐  ┌────────▼────────┐                    │
│  │ MonitorFacade   │  │ WindowMgmt      │                    │
│  │                 │  │ Facade          │                    │
│  └─────────────────┘  └────────┬────────┘                    │
│                                │                              │
│  ┌─────────────────────────────▼──────────┐                  │
│  │ SimulationFacade                        │                  │
│  └─────────────────────────────────────────┘                  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 각 파사드 인터페이스 예시

```csharp
// 1. SerialConnectionFacade
public interface ISerialConnectionFacade
{
    Task ConnectAsync();
    Task DisconnectAsync();
    void RefreshPorts();
    void PerformSmartSelection();
    Task CheckAutoConnectAsync();

    string SerialPort { get; set; }
    bool IsConnected { get; }
    ObservableCollection<ComPortInfo> AvailablePorts { get; }
    string SerialConnectionStatus { get; }
    bool CanConnect { get; }
    bool CanDisconnect { get; }
}

// 2. ConfigurationFacade
public interface IConfigurationFacade
{
    void LoadApiUrl();
    void LoadQueueSettings();
    void SetTransmissionInterval(int interval);
    void SetBatchSize(int batchSize);
    void SetDeviceId(string deviceId);
    void OpenSerialConfig();

    string ApiUrl { get; }
    string TransmissionInterval { get; set; }
    string BatchSize { get; set; }
    string DeviceId { get; set; }
}

// 3. DataMappingFacade
public interface IDataMappingFacade
{
    void AddScenario();
    void DeleteScenario();
    Task TestMappingAsync();
    void SaveMapping();
    void Apply();
    void Cancel();
    string GetApiEndpointForScenario(DataMappingScenario scenario, string data);

    ObservableCollection<DataMappingScenario> MappingScenarios { get; }
    DataMappingScenario? SelectedMappingScenario { get; set; }
    string MappingScenariosCount { get; }
    List<DataSource> DataSources { get; }
    List<TransmissionType> TransmissionTypes { get; }
    List<string> ApiMethods { get; }
    List<string> ContentTypes { get; }

    event EventHandler<bool>? DataMappingWindowCloseRequested;
}

// 4. DataTransmissionFacade
public interface IDataTransmissionFacade
{
    Task OnSerialDataReceivedAsync(byte[] data);
    Task OnMappingProcessedAsync(MappingProcessedEventArgs e);
    Task TestApiAsync();
    Task InitializeQueueProcessingAsync();

    int QueueCount { get; }
    string ApiUrl { get; }
}

// 5. MonitorFacade
public interface IMonitorFacade
{
    void ShowSerialMonitor();
    void HideSerialMonitor();
    void ToggleSerialMonitor();
    void ShowApiMonitor();
    void HideApiMonitor();
    void ToggleApiMonitor();
    void SaveSerialMonitor();
    void SaveApiMonitor();
    void ClearSerialMonitor();
    void ClearApiMonitor();
    void LoadExistingMessages();

    string SerialMonitorText { get; set; }
    string ApiMonitorText { get; set; }
    bool SerialMonitorVisible { get; set; }
    bool ApiMonitorVisible { get; set; }
    // ...기타 모니터 속성들
}

// 6. IWindowManagementFacade
public interface IWindowManagementFacade
{
    void OpenDataMapping();
    void ShowReservedWords();
    void OpenSerialMonitor();
    void OpenApiMonitor();
    void CloseAllChildWindows();
}

// 7. ISimulationFacade
public interface ISimulationFacade
{
    void Start();
    void Stop();
    void Toggle();
    void GenerateSingleData();

    bool IsSimulating { get; }
    string SimulationInterval { get; set; }
    string SimulationButtonText { get; }
}
```

---

## 리팩토링 순서 (권장)

| 순서 | 파사드 | 난이도 | 영향도 | 예상 작업량 |
|------|--------|--------|--------|-----------|
| 1 | **SimulationFacade** | ⭐ | 낮음 | ~0.5일 |
| 2 | **ConfigurationFacade** | ⭐⭐ | 중간 | ~1일 |
| 3 | **MonitorFacade** | ⭐⭐ | 중간 | ~1일 |
| 4 | **SerialConnectionFacade** | ⭐⭐ | 중간 | ~1일 |
| 5 | **DataMappingFacade** | ⭐⭐⭐ | 중간 | ~1.5일 |
| 6 | **WindowManagementFacade** | ⭐⭐⭐ | 낮음 | ~1일 |
| 7 | **DataTransmissionFacade** | ⭐⭐⭐⭐ | 높음 | ~2일 |

> **권장 전략**: 난이도가 낮고 의존성이 적은 파사드부터 점진적으로 분리하여 리스크를 최소화한다.

---

## 점진적 마이그레이션 전략

### Step 1: 인터페이스 및 구현체 파일 생성
```
SimpleSerialToApi/
├── Facades/
│   ├── ISimulationFacade.cs
│   ├── SimulationFacade.cs
│   ├── IConfigurationFacade.cs
│   ├── ConfigurationFacade.cs
│   ├── IMonitorFacade.cs
│   ├── MonitorFacade.cs
│   ├── ISerialConnectionFacade.cs
│   ├── SerialConnectionFacade.cs
│   ├── IDataMappingFacade.cs
│   ├── DataMappingFacade.cs
│   ├── IWindowManagementFacade.cs
│   ├── WindowManagementFacade.cs
│   ├── IDataTransmissionFacade.cs
│   └── DataTransmissionFacade.cs
```

### Step 2: 각 파사드별로 코드 이관
1. 인터페이스 정의
2. 구현체 작성 (MainViewModel에서 관련 코드 복사)
3. MainViewModel에서 해당 코드 제거
4. MainViewModel 생성자에 파사드 주입
5. 빌드 및 테스트

### Step 3: MainViewModel 정리
- Properties → 각 파사드로 위임 또는 제거
- Commands → 각 파사드의 메서드를 직접 호출하도록 변경
- 이벤트 핸들러 → 각 파사드로 이동
- 의존성 13개 → 7개로 감소

---

## 기대 효과

| 지표 | 현재 | 리팩토링 후 |
|------|------|-----------|
| MainViewModel 라인 수 | ~2,130줄 | ~150줄 (파사드 호출 + 커맨드 매핑) |
| 직접 의존성 | 13개 | 7개 (파사드 인터페이스) |
| 단일 책임 | ❌ 위반 | ✅ 준수 |
| 단위 테스트 용이성 | ❌ 어려움 | ✅ 각 파사드별 독립 테스트 가능 |
| 코드 탐색 시간 | ❌ 오래 걸림 | ✅ 책임별 파일 즉시 접근 |
| 병렬 개발 가능 | ❌ 불가능 | ✅ 파사드별 병렬 작업 가능 |
