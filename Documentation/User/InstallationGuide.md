# SimpleSerialToApi 설치 가이드

## 1. 사전 준비사항

### 1.1 시스템 확인

#### Windows 버전 확인
1. **Windows + R** 키 → `winver` 입력 → 확인
2. 지원되는 버전:
   - Windows 10 (1809 이상)
   - Windows 11 (모든 버전)
   - Windows Server 2019/2022

#### 시스템 사양 확인
1. **작업 관리자** → **성능** 탭에서 확인
2. 최소 요구사양:
   - RAM: 4GB 이상
   - 디스크: 100MB 여유 공간
   - 프로세서: x64 아키텍처

#### 관리자 권한 확인
1. Serial 포트 액세스를 위해 관리자 권한 필요
2. 설치 시 관리자 권한으로 실행 필수
3. 권한 확인 방법:
   - **제어판** → **사용자 계정** → **계정 유형 변경**
   - 계정이 "관리자" 유형인지 확인

#### 방화벽 설정 확인
1. **Windows 보안** → **방화벽 및 네트워크 보호**
2. API 통신을 위한 아웃바운드 연결 허용 필요
3. 필요시 방화벽 예외 규칙 추가:
   - **고급 설정** → **아웃바운드 규칙** → **새 규칙**
   - **프로그램**: SimpleSerialToApi.exe
   - **작업**: 연결 허용

### 1.2 Serial 장비 준비

#### 장비 전원 확인
1. Serial 통신 장비의 전원 상태 확인
2. 장비별 초기화 완료 대기
3. 장비 상태 LED 또는 디스플레이 확인

#### 케이블 연결 상태
1. **직렬 통신 케이블 점검**:
   - RS232, RS485, RS422 케이블 타입 확인
   - 케이블 핀 배치 및 연결 상태
   - 케이블 길이 제한 (RS232: 15m, RS485: 1200m)

2. **USB-to-Serial 변환기** (사용 시):
   - 드라이버 설치 상태 확인
   - USB 포트 연결 안정성
   - 변환기 제조사별 호환성 확인

#### COM 포트 번호 확인
1. **장치 관리자** 실행 (`devmgmt.msc`)
2. **포트(COM 및 LPT)** 항목 확장
3. 연결된 Serial 포트 확인:
   - COM1, COM2, COM3... 번호 기록
   - 포트 상태가 "정상 작동" 확인
   - 충돌하는 장치 없음 확인

### 1.3 네트워크 설정

#### API 서버 접근 가능 여부
1. **명령 프롬프트**에서 연결 테스트:
   ```cmd
   ping api.yourserver.com
   telnet api.yourserver.com 80
   curl -I https://api.yourserver.com/health
   ```

2. **방화벽 및 프록시 확인**:
   - 회사 방화벽 정책 확인
   - 프록시 서버 설정 필요 여부
   - 허용해야 할 포트: 80(HTTP), 443(HTTPS)

#### 프록시 설정 (필요시)
1. **Internet Explorer** → **도구** → **인터넷 옵션**
2. **연결** 탭 → **LAN 설정**
3. 프록시 서버 정보 입력:
   - 서버 주소 및 포트
   - 인증이 필요한 경우 자격 증명
   - 로컬 주소에 대해 프록시 서버 사용 안 함 선택

#### 인증서 설정 (HTTPS 사용 시)
1. **자체 서명 인증서**인 경우:
   - **인증서 관리자** (`certmgr.msc`) 실행
   - **신뢰할 수 있는 루트 인증 기관**에 인증서 추가
   
2. **클라이언트 인증서**가 필요한 경우:
   - **개인** 저장소에 클라이언트 인증서 설치
   - 인증서에 개인 키 포함 확인

## 2. 설치 방법

### 2.1 MSI 설치 프로그램 사용

#### Step 1: 설치 파일 다운로드
1. 공식 다운로드 사이트에서 **SimpleSerialToApi-Setup.msi** 다운로드
2. 파일 무결성 확인 (제공된 체크섬과 비교)
3. 바이러스 백신으로 파일 검사

#### Step 2: 설치 실행
1. **SimpleSerialToApi-Setup.msi** 우클릭
2. **관리자 권한으로 실행** 선택
3. UAC 프롬프트에서 **예** 클릭

#### Step 3: 설치 마법사
1. **시작 화면**:
   - 라이선스 동의서 읽기 및 동의
   - **Next** 클릭

2. **설치 유형 선택**:
   - **Typical**: 기본 설치 (권장)
   - **Custom**: 사용자 정의 설치
   - **Complete**: 전체 설치 (개발자용)

3. **설치 위치 선택**:
   - 기본 경로: `C:\Program Files\SimpleSerialToApi`
   - **Browse**로 다른 경로 선택 가능
   - 충분한 디스크 공간 확인

4. **추가 옵션**:
   - [✓] 바탕화면에 바로가기 만들기
   - [✓] 시작 메뉴에 바로가기 만들기  
   - [✓] .NET 8 Runtime 자동 설치 (필요시)

5. **설치 진행**:
   - 파일 복사 진행률 표시
   - .NET Runtime 다운로드 및 설치 (필요시)
   - 레지스트리 항목 생성

6. **설치 완료**:
   - [✓] 설치 완료 후 SimpleSerialToApi 실행
   - **Finish** 클릭

#### Step 4: 설치 확인
1. 시작 메뉴에서 "SimpleSerialToApi" 검색
2. 애플리케이션 실행 확인
3. **도움말** → **정보** 메뉴에서 버전 확인

### 2.2 포터블 버전 사용

#### 포터블 버전 장점
- 설치 없이 바로 실행 가능
- USB 드라이브에서 실행 가능
- 시스템 레지스트리 수정 없음
- 여러 버전 동시 사용 가능

#### Step 1: 압축 파일 다운로드
1. **SimpleSerialToApi-Portable.zip** 다운로드
2. 파일 크기 및 체크섬 확인

#### Step 2: 압축 해제
1. 원하는 폴더에 압축 해제
2. 권장 경로: 
   - `C:\Tools\SimpleSerialToApi`
   - `D:\Applications\SimpleSerialToApi`
3. 폴더 구조 확인:
   ```
   SimpleSerialToApi/
   ├── SimpleSerialToApi.exe
   ├── App.config
   ├── Dependencies/
   │   ├── System.IO.Ports.dll
   │   ├── Newtonsoft.Json.dll
   │   └── ...
   ├── Documentation/
   └── Readme.txt
   ```

#### Step 3: 실행 및 확인
1. **SimpleSerialToApi.exe** 더블클릭 실행
2. 첫 실행 시 Windows 보안 경고 가능:
   - **추가 정보** 클릭
   - **실행** 버튼 클릭
3. 바로가기 생성 (선택사항):
   - 실행 파일 우클릭 → **바로가기 만들기**

### 2.3 개발자 설치 (소스 빌드)

#### 사전 요구사항
- .NET 8 SDK 설치
- Visual Studio 2022 또는 VS Code
- Git 클라이언트

#### 빌드 과정
```bash
# 소스 코드 클론
git clone https://github.com/yourcompany/SimpleSerialToApi.git

# 프로젝트 디렉토리로 이동
cd SimpleSerialToApi

# 패키지 복원
dotnet restore

# 빌드 실행
dotnet build --configuration Release

# 실행
dotnet run --project SimpleSerialToApi --configuration Release
```

## 3. 초기 실행 및 검증

### 3.1 첫 실행
1. 애플리케이션 실행
2. **First Time Setup** 마법사 나타남:
   - Serial 포트 자동 검색
   - 기본 설정 구성
   - 연결 테스트 수행

### 3.2 라이선스 활성화
1. **Help** → **License** 메뉴 선택
2. 라이선스 키 입력 (구매한 경우)
3. 평가판 사용 시 **Trial Mode** 선택
4. 인터넷 연결을 통한 라이선스 검증

### 3.3 기본 설정 확인
1. **Settings** → **Serial Configuration**:
   - 사용 가능한 COM 포트 목록 확인
   - 기본 설정값 확인 (9600, 8, N, 1)

2. **Settings** → **API Configuration**:
   - 샘플 API 엔드포인트 설정
   - 연결 테스트 수행

### 3.4 연결 테스트
1. Serial 장비 연결
2. **Connect** 버튼 클릭
3. **Status** 표시등이 녹색으로 변경되는지 확인
4. **Log** 탭에서 연결 메시지 확인

## 4. 문제 해결

### 4.1 설치 오류

#### "Windows 버전이 지원되지 않습니다"
- **해결방법**: Windows 10 1809 이상으로 업데이트

#### ".NET 8 Runtime을 찾을 수 없습니다"
- **해결방법**: 
  1. https://dotnet.microsoft.com/download/dotnet/8.0 방문
  2. .NET 8 Runtime (x64) 다운로드 및 설치
  3. 재부팅 후 애플리케이션 재실행

#### "설치 권한이 없습니다"
- **해결방법**: 
  1. 관리자 계정으로 로그인
  2. MSI 파일을 관리자 권한으로 실행

### 4.2 실행 오류

#### "COM 포트에 액세스할 수 없습니다"
- **원인**: 다른 프로그램에서 포트 사용 중
- **해결방법**:
  1. **작업 관리자**에서 관련 프로세스 종료
  2. 관리자 권한으로 애플리케이션 실행
  3. 안티바이러스 실시간 보호 일시 해제

#### "API 서버에 연결할 수 없습니다"
- **원인**: 네트워크 연결 또는 방화벽 차단
- **해결방법**:
  1. 인터넷 연결 확인
  2. 방화벽 예외 규칙 추가
  3. 프록시 설정 확인

### 4.3 성능 문제

#### "애플리케이션이 느려집니다"
- **해결방법**:
  1. 로그 레벨을 Warning 이상으로 설정
  2. 큐 크기를 적절히 조정
  3. 사용하지 않는 데이터 수집 비활성화

#### "메모리 사용량이 많습니다"
- **해결방법**:
  1. 로그 파일 크기 제한 설정
  2. 데이터 보관 기간 단축
  3. 배치 크기 축소

## 5. 업그레이드 가이드

### 5.1 자동 업데이트
1. **Help** → **Check for Updates** 메뉴 선택
2. 새 버전 확인 및 다운로드
3. 자동 설치 진행
4. 애플리케이션 재시작

### 5.2 수동 업그레이드
1. 현재 설정 백업:
   - **File** → **Export Settings**
2. 기존 버전 제거 또는 덮어쓰기 설치
3. 새 버전 설치
4. 설정 복원:
   - **File** → **Import Settings**

## 6. 제거 방법

### 6.1 제어판을 통한 제거
1. **제어판** → **프로그램 및 기능**
2. **SimpleSerialToApi** 선택
3. **제거** 클릭
4. 제거 마법사 완료

### 6.2 완전 제거
1. 프로그램 제거 후 남은 파일 삭제:
   - `%APPDATA%\SimpleSerialToApi`
   - `%LOCALAPPDATA%\SimpleSerialToApi`
2. 레지스트리 항목 정리 (고급 사용자만):
   - `HKEY_CURRENT_USER\Software\SimpleSerialToApi`

## 7. 백업 및 복원

### 7.1 설정 백업
정기적으로 다음 파일들을 백업하세요:
- App.config (설정 파일)
- 사용자 정의 매핑 규칙 파일
- 로그 파일 (필요 시)

### 7.2 전체 복원
1. 애플리케이션 재설치
2. 백업한 설정 파일 복원
3. 라이선스 재활성화 (필요 시)

## 8. 기술 지원

설치 관련 문제가 지속될 경우:
- 이메일: support@yourcompany.com  
- 전화: +82-2-xxxx-xxxx
- 온라인 지원: https://support.yourcompany.com
- 설치 로그 파일 첨부하여 문의