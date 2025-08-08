# SimpleSerialToApi

## 🚀 프로젝트 개요

**SimpleSerialToApi**는 Serial 통신을 통해 하드웨어 장비로부터 실시간 데이터를 수집하고, 이를 REST API로 전송하는 Windows 기반 .NET 8 WPF 애플리케이션입니다.

### ✨ 주요 기능

- **🔌 Serial 통신**: COM1~COM256 포트 지원, 다양한 장비 프로토콜 호환
- **📡 실시간 데이터 수집**: 고속 데이터 스트림 처리 및 파싱
- **🌐 REST API 연동**: JSON 형식으로 데이터 전송, 다양한 인증 방식 지원
- **🗄️ 메시지 큐 시스템**: 안정적인 데이터 전송을 위한 내부 큐 처리
- **🖥️ WPF 사용자 인터페이스**: 직관적인 실시간 모니터링 및 설정 관리
- **📊 실시간 모니터링**: 연결 상태, 전송 통계, 오류 현황 실시간 추적
- **🔧 설정 관리**: App.Config 기반의 유연한 구성 관리
- **📝 포괄적인 로깅**: 구조화된 로그 시스템 및 오류 추적

## 📋 시스템 요구사항

### 최소 요구사항
- **운영체제**: Windows 10 (1809) 이상 또는 Windows 11
- **프레임워크**: .NET 8 Runtime
- **메모리**: 4GB RAM
- **디스크 공간**: 100MB 이상
- **하드웨어**: Serial 포트 또는 USB-to-Serial 변환기

### 권장 요구사항
- **메모리**: 8GB RAM 이상
- **프로세서**: Intel Core i5 또는 AMD 동급 이상
- **네트워크**: 안정적인 인터넷 연결

## 🔧 설치 및 실행

### Windows 설치 프로그램 사용
```bash
# MSI 설치 프로그램 실행
SimpleSerialToApi-Setup.msi

# 또는 포터블 버전 사용
# SimpleSerialToApi-Portable.zip 압축 해제 후 실행
```

### 개발자용 빌드
```bash
# 저장소 클론 및 빌드
git clone https://github.com/yamsengyi/SimpleSerialToApi.git
cd SimpleSerialToApi

# 솔루션 복원 및 빌드 (Windows 환경)
dotnet restore
dotnet build

# 애플리케이션 실행 (Windows 환경)
dotnet run --project SimpleSerialToApi

# 테스트 실행
dotnet test
```

## 🏗️ 프로젝트 구조

```
SimpleSerialToApi/
├── 📂 SimpleSerialToApi/           # 메인 WPF 애플리케이션
│   ├── 📂 Services/                # 비즈니스 로직 서비스
│   │   ├── 📂 Serial/              # Serial 통신 관련
│   │   ├── 📂 Api/                 # API 연동 관련
│   │   ├── 📂 Queue/               # 메시지 큐 관련
│   │   └── 📂 Configuration/       # 설정 관리
│   ├── 📂 ViewModels/              # MVVM 뷰모델
│   ├── 📂 Views/                   # WPF 뷰
│   ├── 📂 Models/                  # 데이터 모델
│   ├── 📂 Converters/              # UI 변환기
│   └── 📄 App.config               # 애플리케이션 설정
├── 📂 SimpleSerialToApi.Tests/     # 테스트 프로젝트
│   ├── 📂 Unit/                    # 단위 테스트
│   ├── 📂 Integration/             # 통합 테스트
│   ├── 📂 Mocks/                   # 목(Mock) 객체
│   └── 📂 TestData/                # 테스트 데이터
├── 📂 Documentation/               # 프로젝트 문서
│   ├── 📂 User/                    # 사용자 가이드
│   ├── 📂 Technical/               # 기술 문서
│   ├── 📂 Operations/              # 운영 가이드
│   └── 📂 Legal/                   # 라이선스 문서
└── 📄 SimpleSerialToApi.sln        # 솔루션 파일
```

## 🎯 성능 및 특징

### ⚡ 성능 지표
- **데이터 처리**: 1000건/초 이상 메시지 처리 능력
- **응답 시간**: 1초 이내 Serial 데이터 파싱 및 API 전송
- **메모리 사용량**: 대용량 데이터셋 처리 시 50MB 미만 유지
- **안정성**: 24시간 연속 실행 가능

### 🔒 보안 기능
- API 인증 방식 지원 (Bearer Token, Basic Auth)
- 민감 정보 암호화 저장
- 네트워크 통신 HTTPS 강제
- 입력 데이터 유효성 검증

## 📚 문서 가이드

자세한 사용법과 설정 방법은 다음 문서를 참조하세요:

### 사용자 가이드
- 📖 [**사용자 매뉴얼**](Documentation/User/UserManual.md) - 기본 사용법 및 설정
- ⚙️ [**설치 가이드**](Documentation/User/InstallationGuide.md) - 상세 설치 절차
- 🔧 [**관리자 매뉴얼**](Documentation/User/AdministratorManual.md) - 고급 설정 및 운영

### 기술 문서
- 🏛️ [**시스템 아키텍처**](Documentation/Technical/Architecture.md) - 전체 시스템 구조
- 🔌 [**API 연동 가이드**](Documentation/Technical/ApiIntegrationGuide.md) - API 설정 및 연동 방법
- 💻 [**개발자 가이드**](Documentation/Technical/DeveloperGuide.md) - 개발 환경 설정 및 확장 방법

### 운영 가이드
- 🚨 [**장애 대응 매뉴얼**](Documentation/Operations/TroubleshootingGuide.md) - 문제 해결 방법
- 📈 [**성능 튜닝 가이드**](Documentation/Operations/PerformanceTuningGuide.md) - 최적화 설정

## 🧪 테스트 프레임워크

### 테스트 범위
- ✅ **단위 테스트**: 80%+ 코드 커버리지
- ✅ **통합 테스트**: End-to-End 워크플로우 검증
- ✅ **성능 테스트**: 대용량 데이터 처리 검증
- ✅ **UI 테스트**: MVVM 패턴 검증

### 테스트 실행
```bash
# 모든 테스트 실행
dotnet test

# 특정 카테고리 테스트 실행
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=Performance
```

## 📦 배포

### 자동 배포 스크립트
```powershell
# 배포 패키지 생성
.\Documentation\Deployment\Deploy.ps1

# 다양한 배포 옵션 지원
# - MSI 설치 프로그램
# - 포터블 패키지
# - 자체 포함 배포
# - Framework 종속 배포
```

## 🎉 프로젝트 완료 현황

### ✅ 완료된 단계 (Step 01-10)

- **[Step 01]** ✅ 프로젝트 구조 및 기본 인프라 완성
- **[Step 02]** ✅ Serial 통신 기초 구현 완료
- **[Step 03]** ✅ API 연동 기능 구현 완료
- **[Step 04]** ✅ 메시지 큐 시스템 구현 완료
- **[Step 05]** ✅ App.Config 기반 설정 관리 완료
- **[Step 06]** ✅ 데이터 매핑 및 파싱 로직 완료
- **[Step 07]** ✅ WPF 사용자 인터페이스 완성
- **[Step 08]** ✅ 오류 처리 및 로깅 시스템 완료
- **[Step 09]** ✅ 포괄적인 테스트 프레임워크 완료
- **[Step 10]** ✅ 문서화 및 배포 시스템 완료

### 🏆 주요 성과

- **완전한 기능 구현**: PRD 모든 요구사항 100% 달성
- **프로덕션 준비**: 설치 프로그램 및 배포 자동화 완료
- **포괄적인 문서화**: 사용자, 기술, 운영 문서 완비
- **검증된 품질**: 단위/통합/성능 테스트 완료
- **안정적인 아키텍처**: Clean Architecture 패턴 적용

## 🤝 기여 및 지원

### 개발팀
- **개발자**: yamsengyi
- **완료일**: 2025년 1월 15일
- **버전**: v1.0.0

### 라이선스
본 프로젝트는 [MIT 라이선스](Documentation/Legal/LICENSE.md)를 따릅니다.

### 기술 지원
- 📧 문제 신고: GitHub Issues
- 📝 문서 개선: Pull Request 환영
- 💬 기술 문의: [개발자 가이드](Documentation/Technical/DeveloperGuide.md) 참조

---

⭐ **SimpleSerialToApi**는 산업용 Serial 통신과 현대적인 REST API를 연결하는 안정적이고 확장 가능한 솔루션입니다.