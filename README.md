# SimpleSerialToApi

**SimpleSerialToApi**는 Serial 통신으로 장비 데이터를 수집하고, 시나리오에 따라 매핑한 뒤, 메시지 큐에서 설정된 간격으로 HTTP API 또는 다른 Serial 포트로 전송하는 Windows .NET 8 WPF 애플리케이션입니다.

## 주요 기능

- **Serial 통신**: COM 포트로 장비 연결 및 데이터 수신
- **HTTP API 전송**: 큐 기반 배치 처리, 재시도, 실제 HTTP 응답 모니터링
- **WPF UI**: 연결/큐/전송 상태 및 Serial/API 모니터 실시간 표시
- **시나리오 매핑**: JSON 기반 조건, 우선순위, 데이터 템플릿 및 전송 대상 설정
- **기본 설정**: COM 포트, API 연결 테스트 URL, 배치 전송 간격 설정
- **자동 연결**: 마지막으로 성공한 Serial 포트를 다음 실행 시 자동 연결

## 시스템 요구사항

- **OS**: Windows 10 이상
- **Framework**: .NET 8 Runtime
- **Memory**: 4GB RAM
- **Hardware**: Serial 포트 또는 USB-to-Serial 변환기

## 설치 및 실행

### 📦 사전 빌드된 실행파일 다운로드 (권장)

1. **GitHub Releases**에서 최신 버전 다운로드:
   - [Releases 페이지](https://github.com/yamsengyi/SimpleSerialToApi/releases)에서 `SimpleSerialToApi-vX.X.X-win-x64.zip` 다운로드
   - 압축 해제 후 `SimpleSerialToApi.exe` 실행
   - **장점**: .NET Runtime 설치 불필요 (Self-Contained)

2. **GitHub Actions Artifacts**에서 최신 빌드 다운로드:
   - [Actions 탭](https://github.com/yamsengyi/SimpleSerialToApi/actions)에서 최신 성공한 빌드 선택
   - `SimpleSerialToApi-SelfContained-win-x64` 아티팩트 다운로드

### 🔨 소스코드에서 빌드

```bash
# 저장소 클론
git clone https://github.com/yamsengyi/SimpleSerialToApi.git
cd SimpleSerialToApi

# .NET 8 SDK 설치 확인
dotnet --version  # 8.0.x 이상 필요

# 의존성 복원 및 빌드
dotnet restore
dotnet build --configuration Release

# 실행
dotnet run --project SimpleSerialToApi --configuration Release
```

### 🚀 배포용 빌드 생성

```bash
# Self-Contained 배포 (Runtime 포함)
dotnet publish SimpleSerialToApi/SimpleSerialToApi.csproj -c Release --self-contained true -r win-x64 --output ./publish

# Framework-Dependent 배포 (.NET Runtime 필요)
dotnet publish SimpleSerialToApi/SimpleSerialToApi.csproj -c Release --self-contained false --output ./publish-fd
```

### ⚙️ 시스템 요구사항

- **OS**: Windows 10 이상 (x64)
- **Runtime**:
  - Self-Contained 버전: 없음 (내장됨)
  - Framework-Dependent 버전: [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **권한**: USB 시리얼 통신을 위한 관리자 권한 (자동 등록됨)
- **하드웨어**: Serial 포트 또는 USB-to-Serial 변환기 (FTDI 권장)

## 프로젝트 구조

```
SimpleSerialToApi/
├── Models/
│   ├── ApiModels.cs              # API 데이터 모델
│   ├── ConfigurationModels.cs    # 설정 모델
│   ├── DataMappingModels.cs      # 데이터 매핑 모델
│   ├── DataMappingScenario.cs    # 매핑 시나리오
│   ├── DataModels.cs             # 핵심 데이터 모델
│   ├── MonitorModels.cs          # 모니터 메시지 모델
│   ├── QueueModels.cs            # 큐 데이터 모델
│   └── SerialConnectionSettings.cs # 시리얼 연결 설정
├── Services/
│   ├── SerialCommunicationService.cs # 시리얼 통신 핵심 서비스
│   ├── DataMappingService.cs     # 데이터 매핑 엔진
│   ├── HttpApiClientService.cs   # HTTP API 클라이언트
│   ├── SerialMonitorService.cs   # 시리얼 모니터링
│   ├── ApiMonitorService.cs      # API 모니터링
│   ├── ConfigurationService.cs   # 설정 관리
│   ├── Queues/                   # 메시지 큐 시스템
│   ├── Monitoring/               # 모니터링 서비스
│   ├── Diagnostics/              # 진단 및 로깅
│   └── Recovery/                 # 복구 및 재시도 로직
├── ViewModels/
│   ├── MainViewModel.cs          # 메인 뷰모델 및 화면 바인딩
│   └── RelayCommand.cs           # WPF 명령 구현
├── Views/
│   ├── DataMappingWindow.xaml    # 데이터 매핑 설정 UI
│   ├── SerialConfigWindow.xaml   # 시리얼 설정 UI
│   ├── SerialMonitorWindow.xaml  # 시리얼 모니터 UI
│   ├── ApiMonitorWindow.xaml     # API 모니터 UI
│   └── Controls/                 # 사용자 정의 컨트롤
├── Interfaces/
│   ├── ISerialCommunicationService.cs # 시리얼 통신 인터페이스
│   ├── IApiServices.cs           # API 서비스 인터페이스
│   ├── IDataParsing.cs           # 데이터 파싱 인터페이스
│   └── IMessageQueue.cs          # 메시지 큐 인터페이스
├── Configuration/
│   └── ConfigurationSections.cs  # 설정 섹션 정의
├── Converters/
│   └── StringToVisibilityConverter.cs # WPF 값 변환기
├── MainWindow.xaml/.cs           # 메인 윈도우
├── App.xaml/.cs                  # 앱 엔트리포인트
└── App.config                    # 애플리케이션 설정
```

### 전송 동작

1. Serial 수신 데이터는 `SerialMonitorService`에 기록되고 메시지 단위로 분리됩니다.
2. 활성화된 매핑 시나리오를 우선순위가 높은 순서로 적용합니다. 같은 우선순위에서는 JSON 로드 순서를 유지합니다.
3. API 대상 데이터는 `ApiDataQueue`에 적재됩니다.
4. 큐 처리기는 `QueueTransmissionInterval` 설정 간격마다 배치를 처리하고, 성공/실패한 실제 HTTP 응답을 API 모니터에 기록합니다.
5. 일시적인 HTTP 오류는 재시도하며, 재시도 한도를 초과하면 Dead Letter Queue로 이동합니다.

메인 화면의 `Test API URL`은 연결 테스트 전용입니다. 실제 데이터 전송 URL과 HTTP 메서드는 데이터 매핑 시나리오에서 설정합니다.

## 기술 스택

### 핵심 프레임워크
- **.NET 8 WPF**: UI 프레임워크
- **Microsoft.Extensions.DependencyInjection**: 의존성 주입
- **Microsoft.Extensions.Configuration**: 설정 관리

### 통신 및 네트워킹
- **System.IO.Ports**: Serial 통신
- **Microsoft.Extensions.Http**: HTTP 클라이언트 팩토리
- **Polly**: HTTP 재시도 정책 및 회복탄력성

### 데이터 처리
- **Newtonsoft.Json**: JSON 직렬화/역직렬화
- **System.Configuration.ConfigurationManager**: App.config 관리

### 로깅 및 모니터링
- **Serilog**: 구조화된 로깅
- **Serilog.Sinks.File**: 파일 로그 출력
- **Serilog.Sinks.Console**: 콘솔 로그 출력
- **Serilog.Sinks.EventLog**: Windows 이벤트 로그
- **Microsoft.Extensions.Logging**: 통합 로깅 인터페이스

### UI/UX
- **Microsoft.Xaml.Behaviors.Wpf**: WPF MVVM 동작
- **System.Drawing.Common**: 그래픽 및 이미지 처리

### 시스템 관리
- **System.Management**: Windows 시스템 정보 조회
- **System.Diagnostics.EventLog**: 시스템 이벤트 로그

## 라이선스

자세한 내용은 [LICENSE](doc/LICENSE.md) 참조

---

**개발자**: GitHub Copilot  
**관전자**: yamsengyi  
**작성일**: 2025-08-14
