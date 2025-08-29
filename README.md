# SimpleSerialToApi

**SimpleSerialToApi**는 Serial 통신으로 장비 데이터를 수집하고, 메시지 단위로 Queue에 적재한 뒤, Timer 기반으로 주기적으로 HTTP API로 전송하는 간단하고 안정적인 Windows .NET 8 WPF 애플리케이션입니다.

## 주요 기능

- **Serial 통신**: COM 포트로 장비 연결 및 데이터 수신
- **HTTP API 전송**: Timer 기반 주기적 POST 전송
- **WPF UI**: 연결/큐/전송 상태 실시간 표시
- **기본 설정**: COM 포트, API URL, 전송 주기 설정

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
│   ├── MainViewModel.cs          # 메인 뷰모델
│   ├── SettingsViewModel.cs      # 설정 뷰모델
│   ├── SerialStatusViewModel.cs  # 시리얼 상태 뷰모델
│   ├── ApiStatusViewModel.cs     # API 상태 뷰모델
│   └── QueueStatusViewModel.cs   # 큐 상태 뷰모델
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
│   └── StatusConverters.cs       # WPF 값 변환기
├── MainWindow.xaml/.cs           # 메인 윈도우
├── App.xaml/.cs                  # 앱 엔트리포인트
└── App.config                    # 애플리케이션 설정
```

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
