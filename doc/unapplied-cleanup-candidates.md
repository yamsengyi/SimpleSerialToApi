# 미적용 정리 후보

작성일: 2026-07-23

이 문서는 UI 도달성 기준 정리에서 **삭제를 적용하지 않은** 후보를 기록한다.
각 항목은 현재 코드에 참조 또는 사용자 노출 경로가 있어, 단순한 0-참조 삭제 대상으로 처리하지 않았다.

## 적용 기준

- 실제 앱 시작 경로, DI 등록, XAML 바인딩, 런타임 폴백 경로를 확인했다.
- 참조가 없거나 앱 도달 경로가 없는 종말단 코드만 이번 정리에서 삭제했다.
- 아래 항목은 동작 변경 또는 설정 UX 변경이 수반될 수 있어 별도 작업으로 남긴다.

## 후보

### 1. `SimpleQueueService`의 레거시 문자열 큐 API

대상: `SimpleSerialToApi/Services/SimpleQueueService.cs`

현재 실사용 경로는 `ExtractMessages()`와 `IsFrameInProgress`이며, `MainViewModel`이 수신 프레임을 분리하는 데 사용한다.
반면 아래 문자열 큐 API는 프로덕션 호출자를 찾지 못했다.

- `Enqueue`
- `TryDequeue`
- `DequeueAll`
- `Count`
- `IsEmpty`
- `ClearQueue`
- `ParseAndEnqueue`

권장 작업:

1. `SimpleQueueService`를 프레임 추출 전용 서비스로 축소한다.
2. 위 레거시 API와 내부 `ConcurrentQueue<string>`를 삭제한다.
3. 분할 프레임, 연속 프레임, STX/ETX 밖의 데이터에 대한 단위 테스트를 먼저 추가한다.

보류 사유: 현재 클래스가 DI로 주입되고 있으므로, 프레임 추출 동작을 보장하는 테스트 없이 API를 축소하면 향후 외부 확장 또는 플러그인 호출을 깨뜨릴 수 있다.

### 2. 전송 간격/배치 크기 UI 설정과 실제 큐 처리 정책의 불일치

대상:

- `SimpleSerialToApi/MainWindow.xaml`
- `SimpleSerialToApi/ViewModels/MainViewModel.cs`
- `SimpleSerialToApi/App.config`
- `SimpleSerialToApi/Services/Queues/ApiDataQueueProcessor.cs`
- `SimpleSerialToApi/Services/Queues/QueueManager.cs`

`TransmissionInterval`, `BatchSize`와 두 저장 명령은 UI에서 사용되며 `App.config`의 `QueueTransmissionInterval`, `QueueBatchSize` 값을 갱신한다.
하지만 실제 큐 배치 처리는 `ApiDataQueueProcessor.MaxBatchSize => 50`을 사용하고, 검색 범위에서는 저장된 두 설정값을 큐 실행 정책이 읽어 적용하는 경로를 찾지 못했다.

권장 작업 중 하나를 선택한다.

1. 설정을 유지한다: `IQueueProcessor` 또는 `QueueManager`가 설정을 읽어 배치 크기와 폴링/전송 간격에 실제 반영하도록 구현한다.
2. 설정을 제거한다: 관련 UI 컨트롤, `MainViewModel` 속성/명령, `App.config` 키 및 설정 로드 코드를 함께 삭제한다.

보류 사유: UI에 노출된 설정이므로 단순 삭제는 사용자 기능을 제거한다. 제품 요구 사항으로 전송 간격과 배치 크기가 필요한지 먼저 결정해야 한다.

### 3. 사용되지 않는 `QueueHealthChanged` 이벤트 계약

대상:

- `SimpleSerialToApi/Interfaces/IQueueManager.cs`
- `SimpleSerialToApi/Services/Queues/QueueManager.cs`

`IQueueManager.QueueHealthChanged` 이벤트는 인터페이스와 구현체에 선언되어 있으나, 현재 구현에서 발생시키거나 구독하는 경로를 찾지 못했다. Release 빌드에서도 `QueueManager.QueueHealthChanged` 미사용 경고(CS0067)가 발생한다.

권장 작업:

1. 큐 상태 알림이 필요하면 상태 전이 지점에서 이벤트를 발생시키고 UI 또는 로깅 서비스가 구독하도록 연결한다.
2. 상태 알림이 필요 없으면 인터페이스와 구현체에서 함께 제거한다.

보류 사유: 공개 인터페이스 계약이므로 구현체만 제거하면 안 되며, 외부 소비 가능성을 확인한 뒤 함께 변경해야 한다.

### 4. `QueueManager`의 미사용 예외 변수

대상: `SimpleSerialToApi/Services/Queues/QueueManager.cs`

Release 빌드에서 세 곳의 `catch (Exception ex)`가 `ex`를 사용하지 않아 CS0168 경고가 발생한다.

권장 작업:

1. 예외 세부 정보가 필요하면 `_logger.Log...`에 예외를 전달한다.
2. 의도적으로 무시하는 경우 `catch (Exception)`으로 바꾼다.

보류 사유: 기능상 고아 코드는 아니며, 예외를 로그에 남겨야 하는지 판단이 선행되어야 한다.

## 후보에서 제외한 항목

`SimpleHttpService`와 `ProcessApiTransmissionFallback`은 큐 전송 실패 또는 미초기화 상황의 직접 전송 폴백 경로에서 사용된다. 단순 참조 수가 적더라도 현재 전송 실패 대응에 영향을 주므로 삭제 후보로 분류하지 않았다.