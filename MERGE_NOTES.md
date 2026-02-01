# 병합 주의사항 (Merge Considerations)

## 개요 (Overview)
이 PR은 JSON 설정 파일(`data-mapping-scenarios.json`)이 설정 변경 후 즉시 다시 로드되지 않는 문제를 해결합니다.

**Issue**: 기존 저장된 JSON을 설정변경후 불러오기 했을때 즉시 불러오지 않고 재시작이 필요함

## 변경 사항 (Changes Made)

### 1. DataMappingService.cs
- **추가된 메서드**: `ReloadScenariosFromFile()` 
  - 기존의 private `LoadScenariosFromFile()` 메서드를 호출하는 public wrapper
  - 외부에서 JSON 파일로부터 시나리오를 다시 로드할 수 있도록 함
  - 로드 성공 시 로그 메시지 추가

### 2. MainViewModel.cs
#### 2.1 생성자 (Constructor)
- **추가**: `ConfigurationChanged` 이벤트 구독
  ```csharp
  _configurationService.ConfigurationChanged += OnConfigurationChanged;
  ```

#### 2.2 새로운 이벤트 핸들러
- **추가**: `OnConfigurationChanged()` 메서드
  - 설정이 변경될 때 자동으로 JSON 파일에서 시나리오를 다시 로드
  - UI 스레드에서 `InitializeMappingScenarios()` 호출하여 UI 동기화
  - 사용자에게 상태 메시지 표시

#### 2.3 OpenDataMapping() 메서드
- **추가**: DataMappingWindow를 열기 전 JSON 파일 리로드
  - 창을 열 때마다 최신 데이터 표시 보장
  - 외부에서 JSON 파일을 수정한 경우에도 반영됨

#### 2.4 Dispose() 메서드
- **추가**: 이벤트 구독 해제
  ```csharp
  _configurationService.ConfigurationChanged -= OnConfigurationChanged;
  ```

## 동작 방식 (How It Works)

### 시나리오 1: 설정 변경 시 자동 리로드
1. 사용자가 App.config 설정을 변경 (예: 시리얼 포트 설정)
2. `ConfigurationService.SaveSerialSettings()` 호출
3. `ConfigurationChanged` 이벤트 발생
4. `OnConfigurationChanged()` 핸들러 실행
5. JSON 파일에서 시나리오 자동 리로드
6. UI에 최신 데이터 표시

### 시나리오 2: DataMappingWindow 열 때 리로드
1. 사용자가 "Data Mapping" 메뉴 클릭
2. `OpenDataMapping()` 메서드 실행
3. JSON 파일에서 최신 시나리오 리로드
4. UI 컬렉션 동기화
5. 창 표시

## 병합 시 충돌 가능성 (Potential Merge Conflicts)

### 높은 충돌 위험 파일 (High Risk)
1. **MainViewModel.cs**
   - 이유: 이 파일은 애플리케이션의 핵심 ViewModel로 많은 기능이 추가/수정될 수 있음
   - 충돌 영역:
     - 생성자 부분 (이벤트 구독 추가)
     - 이벤트 핸들러 섹션 (새로운 메서드 추가)
     - `OpenDataMapping()` 메서드 (리로드 로직 추가)
     - `Dispose()` 메서드 (구독 해제 추가)

### 중간 충돌 위험 파일 (Medium Risk)
2. **DataMappingService.cs**
   - 이유: 매핑 기능 개선 작업이 진행될 수 있음
   - 충돌 영역:
     - `LoadScenariosFromFile()` 메서드 수정 (로그 추가)
     - 새로운 public 메서드 `ReloadScenariosFromFile()` 추가

## 충돌 해결 가이드 (Conflict Resolution Guide)

### MainViewModel.cs 충돌 시

#### 1. 생성자에서 이벤트 구독
**우선순위: 필수 유지**
```csharp
// 반드시 포함되어야 함:
_configurationService.ConfigurationChanged += OnConfigurationChanged;
```
- 다른 이벤트 구독들과 함께 배치
- 주석 "// 설정 변경 이벤트 구독 - JSON 자동 리로드" 유지 권장

#### 2. OnConfigurationChanged() 메서드
**우선순위: 필수 유지**
- 다른 이벤트 핸들러들 (`OnQueueProcessed`, `OnMappingProcessed` 등) 근처에 배치
- 전체 메서드 유지 필요

#### 3. OpenDataMapping() 메서드 내 리로드 로직
**우선순위: 권장 유지**
```csharp
// 창을 열기 전에 JSON에서 최신 시나리오를 리로드
if (_dataMappingService.ReloadScenariosFromFile())
{
    InitializeMappingScenarios();
    _logger.LogInformation("Mapping scenarios reloaded from file when opening DataMappingWindow");
}
```
- 새 창 생성 직후, 이벤트 핸들러 등록 전에 배치
- 다른 초기화 로직과 충돌 시 해당 로직 이후에 배치

#### 4. Dispose() 메서드 내 구독 해제
**우선순위: 필수 유지**
```csharp
if (_configurationService != null)
{
    _configurationService.ConfigurationChanged -= OnConfigurationChanged;
}
```
- Dispose 메서드 최상단, 백그라운드 작업 취소 전에 배치

### DataMappingService.cs 충돌 시

#### ReloadScenariosFromFile() 메서드
**우선순위: 필수 유지**
- Public 메서드 섹션에 배치
- `LoadScenariosFromFile()` 메서드 바로 위에 배치 권장

#### LoadScenariosFromFile() 로그 추가
**우선순위: 권장 유지**
```csharp
_logger.LogInformation("Successfully reloaded {Count} scenarios from file", _scenarios.Count);
```
- 다른 로그와 일관성 유지를 위해 포함 권장
- 충돌 시 생략 가능 (핵심 기능에 영향 없음)

## 테스트 체크리스트 (Testing Checklist)

병합 후 다음 사항들을 테스트해야 합니다:

### 1. 설정 변경 후 자동 리로드
- [ ] 시리얼 포트 설정을 변경하고 저장
- [ ] 상태 바에 "Configuration reloaded - mapping scenarios updated from file" 메시지 확인
- [ ] DataMappingWindow를 열어서 최신 시나리오가 표시되는지 확인

### 2. DataMappingWindow 열 때 리로드
- [ ] 매핑 시나리오를 저장
- [ ] DataMappingWindow 닫기
- [ ] 외부 편집기로 `data-mapping-scenarios.json` 파일 수정
- [ ] DataMappingWindow 다시 열기
- [ ] 수정된 내용이 반영되었는지 확인

### 3. 시나리오 저장 및 재로드
- [ ] 새로운 매핑 시나리오 추가
- [ ] "저장" 버튼 클릭
- [ ] 설정 변경 (예: 시리얼 포트)
- [ ] 시나리오가 유지되는지 확인

### 4. 메모리 누수 방지
- [ ] 애플리케이션 실행
- [ ] 여러 번 설정 변경
- [ ] 여러 번 DataMappingWindow 열고 닫기
- [ ] 애플리케이션 종료
- [ ] 오류나 경고 로그 확인

## 호환성 (Compatibility)

### 기존 코드와의 호환성
- ✅ 기존 API 변경 없음 (새로운 메서드만 추가)
- ✅ 기존 동작 방식 유지
- ✅ 하위 호환성 보장

### 다른 브랜치와의 호환성 고려사항
- `DataMappingService`에 새로운 매핑 기능이 추가되는 경우
  - `ReloadScenariosFromFile()` 메서드가 영향을 받을 수 있음
  - `LoadScenariosFromFile()`의 변경사항을 `ReloadScenariosFromFile()`에도 반영 필요
  
- `MainViewModel`에 새로운 이벤트 핸들러가 추가되는 경우
  - `OnConfigurationChanged()` 메서드와 배치 순서 조정 필요
  - Dispose 패턴이 수정되는 경우 구독 해제 로직 재검토 필요

## 성능 영향 (Performance Impact)

### 긍정적 영향
- 애플리케이션 재시작 불필요 → 사용자 경험 개선
- 즉각적인 설정 반영 → 생산성 향상

### 잠재적 우려사항
- 설정 변경마다 JSON 파일 읽기 발생
  - **완화**: JSON 파일 크기가 작고 (최대 10개 시나리오) 빈도가 낮음
  - **완화**: 파일이 없는 경우 빠른 실패 (File.Exists 체크)

- DataMappingWindow 열 때마다 파일 읽기
  - **완화**: 창을 자주 열지 않는 일반적인 사용 패턴
  - **완화**: 파일 I/O는 비동기가 아니지만 매우 빠름 (< 10ms)

## 추가 개선 제안 (Future Improvements)

1. **파일 시스템 와처 (FileSystemWatcher)**
   - JSON 파일이 외부에서 변경될 때 자동 리로드
   - 현재는 의도적으로 제거됨 (파일 잠금 문제 방지)

2. **비동기 파일 로딩**
   - 대용량 JSON 파일을 지원하는 경우
   - `async/await` 패턴 적용

3. **로드 실패 시 사용자 알림**
   - 현재는 로그만 기록
   - MessageBox 또는 Toast 알림 추가 고려

4. **설정 변경 종류별 필터링**
   - 모든 설정 변경이 아닌 관련 설정 변경 시에만 리로드
   - 성능 최적화 가능

## 문의 (Questions)

병합 중 문제가 발생하거나 추가 설명이 필요한 경우:
- PR 코멘트로 질문
- 이 문서의 내용을 참조하여 해결

---

**작성일**: 2026-02-01  
**버전**: 1.0  
**관련 이슈**: 기존 저장된 JSON을 설정변경후 불러오기 했을때 즉시 불러오지 않고 재시작이 필요함
