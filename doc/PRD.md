# 제품 요구 사항 명세서 (PRD) - 핵심 기능 중심
## 프로젝트명: SimpleSerialToApi (dotnet 8 기반)

---

### 1. 개요
Windows 환경에서 Serial 통신으로 장비 데이터를 수집하고, STX/ETX 기반 메시지 단위로 내부 Queue에 적재한 뒤, Timer 기반으로 주기적으로 HTTP API로 전송하는 간단하고 안정적인 로컬 애플리케이션을 개발한다.

---

### 2. 핵심 기능

#### 2.1 Serial 통신
- 사용자가 지정한 시리얼 포트(COM3, COM4 등)로 장비와 연결
- 기본 시리얼 포트 설정: baudrate, parity, stopbits
- 실시간 Serial 데이터 수신 및 연결 상태 표시 

#### 2.2 데이터 Queue 관리
- STX/ETX 기반 메시지 파싱 및 완전한 메시지 단위로 Queue 적재
- 간단한 FIFO Queue로 메시지 순차 처리
- Queue 상태(대기 건수) 실시간 표시

#### 2.3 HTTP API 전송
- Timer 기반 주기적 Queue 처리 (설정 가능한 간격)
- Queue에서 메시지를 가져와 HTTP POST로 전송
- 기본적인 인증 지원 (Bearer Token 또는 Basic Auth)
- 전송 성공/실패 상태 표시

#### 2.4 WPF UI
- Serial 연결 상태 표시 (연결됨/끊어짐)
- Queue 상태 표시 (대기 건수)
- API 전송 상태 표시 (마지막 전송 시간, 성공/실패)
- 기본 설정 입력 (COM 포트, API URL, 전송 주기)

---

### 3. 단순화된 요구사항

#### 3.1 성능
- 기본적인 실시간 처리 (과도한 성능 최적화 제외)
- Queue 처리량: 일반적인 사용 범위에서 안정적 동작

#### 3.2 안정성
- 기본적인 예외 처리 및 에러 로깅
- Serial 포트 재연결 시도
- HTTP 전송 실패 시 간단한 재시도

#### 3.3 설정 관리
- App.Config 기반 기본 설정
- 런타임 설정 변경 가능 (UI를 통해)

---

### 4. 제외된 복잡한 기능
- ❌ 고급 Health Monitor / Diagnostics
- ❌ 복잡한 Recovery Strategies  
- ❌ 과도한 Configuration 시스템
- ❌ 복합적인 Logging/Event 시스템
- ❌ 고급 보안 기능 (기본 인증만 지원)

---

### 5. 기술 스택
- .NET 8 WPF (C#)
- System.IO.Ports (Serial 통신)
- HttpClient (HTTP 전송)
- System.Timers (주기적 처리)
- 최소한의 외부 의존성

---

### 6. 단순화된 아키텍처
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

### 7. 구현 우선순위
1. **Phase 1**: Serial 통신 + 기본 Queue
2. **Phase 2**: HTTP 전송 + Timer
3. **Phase 3**: WPF UI 완성
4. **Phase 4**: 기본 설정 관리

---

**목표:** 간단하고 안정적인 Serial → Queue → HTTP 시스템  
**작성자:** yamsengyi  
**작성일:** 2025-08-14 (단순화 버전)
