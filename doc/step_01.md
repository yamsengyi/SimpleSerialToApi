# Step 01: 프로젝트 설정 및 기본 구조

## 개요
.NET 8 기반 WPF 애플리케이션의 기본 프로젝트 구조를 생성하고 필요한 NuGet 패키지를 설정합니다.

## 상세 작업

### 1.1 프로젝트 생성
- .NET 8 WPF 애플리케이션 프로젝트 생성
- 솔루션 구조 설정 (메인 프로젝트 + 테스트 프로젝트)
- Git 설정 및 .gitignore 확인

### 1.2 프로젝트 구조 설계
```
SimpleSerialToApi/
├── SimpleSerialToApi/                 # 메인 WPF 프로젝트
│   ├── Models/                       # 데이터 모델
│   ├── Services/                     # 비즈니스 로직 서비스
│   │   ├── Serial/                   # Serial 통신 관련
│   │   ├── Api/                      # API 연동 관련
│   │   ├── Queue/                    # Message Queue 관련
│   │   └── Configuration/            # 설정 관리
│   ├── ViewModels/                   # MVVM ViewModels
│   ├── Views/                        # WPF Views
│   ├── Utils/                        # 유틸리티 클래스
│   └── App.config                    # 애플리케이션 설정
├── SimpleSerialToApi.Tests/          # 단위 테스트 프로젝트
└── SimpleSerialToApi.sln             # 솔루션 파일
```

### 1.3 필수 NuGet 패키지 추가
- **System.IO.Ports**: Serial 통신
- **Microsoft.Extensions.Configuration**: 설정 관리
- **Microsoft.Extensions.Logging**: 로깅
- **Microsoft.Extensions.DependencyInjection**: DI 컨테이너
- **Newtonsoft.Json**: JSON 직렬화
- **System.Collections.Concurrent**: 동시성 컬렉션 (Queue용)

### 1.4 테스트 프로젝트 설정
- **Microsoft.NET.Test.Sdk**
- **xUnit** 또는 **MSTest**
- **Moq**: Mocking 프레임워크
- **FluentAssertions**: 테스트 어설션

## 기술 요구사항
- .NET 8 WPF 프로젝트
- MVVM 패턴 적용 준비
- Dependency Injection 컨테이너 설정
- 로깅 시스템 기초 구조

## 산출물
- [x] 솔루션 파일 (.sln)
- [x] 메인 WPF 프로젝트
- [x] 단위 테스트 프로젝트
- [x] 기본 폴더 구조 생성
- [x] NuGet 패키지 설치 완료
- [x] 기본 App.config 파일

## 완료 조건
1. 프로젝트가 빌드 오류 없이 컴파일 됨
2. 기본 WPF 창이 실행됨
3. 테스트 프로젝트가 실행됨
4. 모든 필수 NuGet 패키지가 설치됨
5. 로깅 시스템이 기본 설정됨

## 다음 단계 의존성
이 단계가 완료되어야 Step 02 (Serial 통신 기초)를 시작할 수 있습니다.

## 예상 소요 시간
**2-3시간**

## 담당자 역할
- **개발자**: 프로젝트 생성, 패키지 설치, 기본 구조 설정
- **검토자**: 프로젝트 구조 및 패키지 선택 검토