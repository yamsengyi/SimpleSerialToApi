# SimpleSerialToApi

**SimpleSerialToApi**는 Serial 통신으로 장비 데이터를 수집하고, STX/ETX 기반 메시지 단위로 Queue에 적재한 뒤, Timer 기반으로 주기적으로 HTTP API로 전송하는 간단하고 안정적인 Windows .NET 8 WPF 애플리케이션입니다.

## 주요 기능

- **Serial 통신**: COM 포트로 장비 연결 및 데이터 수신
- **STX/ETX Queue**: 메시지 단위 파싱 및 FIFO 큐 관리
- **HTTP API 전송**: Timer 기반 주기적 POST 전송
- **WPF UI**: 연결/큐/전송 상태 실시간 표시
- **기본 설정**: COM 포트, API URL, 전송 주기 설정

## 시스템 요구사항

- **OS**: Windows 10 이상
- **Framework**: .NET 8 Runtime
- **Memory**: 4GB RAM
- **Hardware**: Serial 포트 또는 USB-to-Serial 변환기

## 설치 및 실행

### 빌드 및 실행
```bash
# 저장소 클론
git clone https://github.com/yamsengyi/SimpleSerialToApi.git
cd SimpleSerialToApi

# 빌드 및 실행
dotnet restore
dotnet build
dotnet run --project SimpleSerialToApi
```

## 프로젝트 구조

```
SimpleSerialToApi/
├── Models/
│   ├── SerialData.cs          # 시리얼 데이터 모델
│   └── QueueItem.cs           # 큐 아이템 모델
├── Services/
│   ├── SerialService.cs       # 시리얼 통신
│   ├── QueueService.cs        # 간단한 큐
│   ├── HttpService.cs         # HTTP 전송
│   └── TimerService.cs        # 타이머
├── ViewModels/
│   └── MainViewModel.cs       # 메인 뷰모델
├── MainWindow.xaml/.cs        # 메인 윈도우
└── App.xaml/.cs              # 앱 엔트리
```

## 기술 스택

- **.NET 8 WPF**: UI 프레임워크
- **System.IO.Ports**: Serial 통신
- **HttpClient**: HTTP API 전송
- **System.Timers**: 주기적 처리

## 라이선스

MIT License - 자세한 내용은 [LICENSE](doc/LICENSE.md) 참조

---

**개발자**: github copilot(Claude Sonnet4)/github copilot coding agent | **관전자**: yamsengyi | **작성일**: 2025-08-14