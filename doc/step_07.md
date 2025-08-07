# Step 07: WPF UI 개발

## 개요
Serial 연결 상태, API 전송 상태, 로그 및 설정을 관리할 수 있는 WPF 사용자 인터페이스를 구현합니다. MVVM 패턴을 적용하여 유지보수성을 확보합니다.

## 상세 작업

### 7.1 MVVM 아키텍처 구성
- `ViewModelBase` 기본 클래스 구현 (INotifyPropertyChanged)
- `RelayCommand` 클래스 구현 (ICommand)
- `Messenger` 또는 `EventAggregator` 구현 (ViewModel 간 통신)
- View-ViewModel 바인딩 설정

### 7.2 메인 윈도우 구성
```xml
<!-- MainWindow.xaml 레이아웃 -->
<Window x:Class="SimpleSerialToApi.Views.MainWindow">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 메뉴바 -->
            <RowDefinition Height="*"/>    <!-- 메인 컨텐츠 -->
            <RowDefinition Height="200"/>  <!-- 로그 패널 -->
            <RowDefinition Height="Auto"/> <!-- 상태바 -->
        </Grid.RowDefinitions>
        
        <!-- 메뉴 및 툴바 -->
        <Menu Grid.Row="0"/>
        
        <!-- 탭 컨트롤 (대시보드, 설정, 모니터링) -->
        <TabControl Grid.Row="1"/>
        
        <!-- 로그 뷰어 -->
        <Expander Grid.Row="2" Header="Logs"/>
        
        <!-- 상태바 (연결 상태, 처리 통계) -->
        <StatusBar Grid.Row="3"/>
    </Grid>
</Window>
```

### 7.3 대시보드 화면
- **연결 상태 패널**: Serial 포트 연결 상태 표시
- **API 상태 패널**: 각 API 엔드포인트 상태 표시
- **실시간 통계**: 메시지 처리량, 성공/실패율
- **큐 상태**: 현재 대기 중인 메시지 수
- **시작/중지 버튼**: 애플리케이션 실행 제어

### 7.4 설정 관리 화면
```csharp
// 설정 뷰모델
public class SettingsViewModel : ViewModelBase
{
    public SerialSettingsViewModel SerialSettings { get; set; }
    public ApiSettingsViewModel ApiSettings { get; set; }
    public QueueSettingsViewModel QueueSettings { get; set; }
    
    public ICommand SaveSettingsCommand { get; set; }
    public ICommand LoadSettingsCommand { get; set; }
    public ICommand TestConnectionCommand { get; set; }
}
```

#### 7.4.1 Serial 포트 설정 섹션
- COM 포트 선택 (드롭다운)
- Baud Rate, Parity, Data Bits, Stop Bits 설정
- 연결 테스트 버튼
- 장비 초기화 설정

#### 7.4.2 API 엔드포인트 설정 섹션
- API URL, 메서드, 인증 설정
- 헤더 및 파라미터 설정
- API 연결 테스트 기능
- 매핑 규칙 편집기

#### 7.4.3 Queue 설정 섹션
- Queue 크기, 배치 크기 설정
- 재시도 정책 설정
- 처리 스레드 수 설정

### 7.5 모니터링 및 로그 화면
- **실시간 로그 뷰어**: 필터링 및 검색 기능
- **성능 차트**: 처리량, 응답시간 그래프
- **오류 알림 패널**: 중요한 오류 및 경고 표시
- **통계 대시보드**: 일/주/월 통계

### 7.6 주요 ViewModel 클래스

#### MainViewModel
```csharp
public class MainViewModel : ViewModelBase
{
    public SerialStatusViewModel SerialStatus { get; set; }
    public ApiStatusViewModel ApiStatus { get; set; }
    public QueueStatusViewModel QueueStatus { get; set; }
    public LogViewModel LogViewer { get; set; }
    
    public ICommand StartApplicationCommand { get; set; }
    public ICommand StopApplicationCommand { get; set; }
    public ICommand OpenSettingsCommand { get; set; }
    
    public bool IsApplicationRunning { get; set; }
    public string ApplicationStatus { get; set; }
}
```

#### SerialStatusViewModel
```csharp
public class SerialStatusViewModel : ViewModelBase
{
    public string ConnectionStatus { get; set; }
    public string PortName { get; set; }
    public string BaudRate { get; set; }
    public DateTime LastDataReceived { get; set; }
    public int TotalMessagesReceived { get; set; }
    
    public ICommand ConnectCommand { get; set; }
    public ICommand DisconnectCommand { get; set; }
    public ICommand SendTestCommand { get; set; }
}
```

### 7.7 사용자 경험 향상 기능
- **다크/라이트 테마** 지원
- **창 상태 저장**: 크기, 위치 기억
- **실시간 업데이트**: 상태 변경 시 즉시 반영
- **키보드 단축키**: 주요 기능 빠른 접근
- **도구 팁**: 사용법 안내

### 7.8 데이터 바인딩 및 변환기
```csharp
// 상태를 색상으로 변환
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => Brushes.Green,
                ConnectionStatus.Connecting => Brushes.Orange,
                ConnectionStatus.Disconnected => Brushes.Red,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }
}
```

## 기술 요구사항
- WPF (.NET 8)
- MVVM 패턴
- Data Binding
- Command Pattern
- ObservableCollection
- WPF Charts (LiveCharts 등)

## 주요 UI 컴포넌트

### 커스텀 컨트롤
- `StatusIndicator`: LED 스타일 상태 표시기
- `LogViewer`: 고성능 로그 뷰어 컨트롤
- `ProgressRing`: 처리 중 상태 표시
- `NotificationPanel`: 알림 메시지 패널

### 필요한 NuGet 패키지
- **LiveCharts.Wpf**: 차트 컨트롤
- **MaterialDesignThemes**: Material Design UI
- **MahApps.Metro**: 모던 UI 테마
- **Microsoft.Xaml.Behaviors.Wpf**: MVVM 동작

## 산출물
- [x] 메인 윈도우 및 XAML 레이아웃
- [x] 모든 ViewModel 클래스들
- [x] 설정 관리 화면 및 바인딩
- [x] 대시보드 및 모니터링 화면
- [x] 로그 뷰어 컨트롤
- [x] 커스텀 컨트롤들
- [x] 데이터 변환기 클래스들
- [x] UI 스타일 및 테마
- [x] 사용자 설정 저장/로드 기능

## 완료 조건
1. 애플리케이션이 정상적으로 실행되고 UI가 표시됨
2. Serial 연결 상태가 실시간으로 업데이트됨
3. API 전송 상태 및 통계가 정확히 표시됨
4. 설정 변경이 즉시 적용됨
5. 로그가 실시간으로 업데이트되고 필터링됨
6. 모든 버튼 및 메뉴가 정상 동작함
7. 창 크기 조정 및 테마 변경이 동작함
8. UI 응답성이 양호함 (UI 스레드 블로킹 없음)

## 사용자 인터페이스 요구사항
- **직관적 조작**: 비개발자도 쉽게 사용 가능
- **실시간 피드백**: 모든 동작에 즉각적인 피드백 제공
- **오류 안내**: 오류 발생 시 명확한 메시지 표시
- **접근성**: 키보드 네비게이션 및 스크린 리더 지원

## 다음 단계 의존성
이 단계가 완료되어야 Step 08 (오류 처리 및 로깅)을 진행할 수 있습니다.

## 예상 소요 시간
**4-5일 (32-40시간)**

## 주의사항
- UI 스레드에서 장시간 작업 수행 금지
- 메모리 바인딩 해제로 메모리 누수 방지
- 대량 데이터 표시 시 가상화 활용
- 반응형 디자인 고려 (다양한 화면 크기)

## 담당자 역할
- **UI/UX 개발자**: XAML 레이아웃 및 스타일 개발
- **프론트엔드 개발자**: ViewModel 및 바인딩 로직 구현
- **디자이너**: UI 디자인 및 사용자 경험 설계
- **검토자**: 사용성 및 접근성 검토