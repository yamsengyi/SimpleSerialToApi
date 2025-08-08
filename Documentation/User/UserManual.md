# SimpleSerialToApi 사용자 매뉴얼

## 1. 개요
SimpleSerialToApi는 Serial 통신으로 장비 데이터를 수집하여 REST API로 전송하는 Windows 애플리케이션입니다.

### 주요 기능
- 다양한 Serial 장비와의 통신 지원 (COM1~COM256)
- 실시간 데이터 수집 및 파싱
- REST API 전송 (JSON 형식)
- 메시지 큐 시스템을 통한 안정적 데이터 처리
- 직관적인 WPF 사용자 인터페이스
- 실시간 모니터링 및 로깅

## 2. 시스템 요구사항

### 최소 요구사항
- **운영체제**: Windows 10/11 (64-bit)
- **프레임워크**: .NET 8 Runtime
- **메모리**: 최소 4GB RAM
- **디스크 공간**: 100MB 이상
- **네트워크**: 인터넷 연결 (API 통신용)

### 권장 요구사항
- **메모리**: 8GB RAM 이상
- **프로세서**: Intel Core i5 또는 AMD 동급 이상
- **네트워크**: 안정적인 인터넷 연결

### 하드웨어 요구사항
- Serial 포트 (COM1~COM256) 또는 USB-to-Serial 변환기
- Serial 통신 케이블
- 지원되는 Serial 장비

## 3. 설치 방법

### 3.1 설치 프로그램 사용
1. **SimpleSerialToApi-Setup.msi** 실행
2. 설치 마법사 안내에 따라 진행
3. 설치 위치 선택 (기본값: C:\Program Files\SimpleSerialToApi)
4. .NET 8 Runtime 자동 설치 (필요시)
5. 바탕화면 바로가기 생성 선택
6. 설치 완료

### 3.2 포터블 버전 사용
1. **SimpleSerialToApi-Portable.zip** 압축 해제
2. 원하는 폴더에 압축 해제
3. SimpleSerialToApi.exe 실행

### 3.3 설치 확인
1. 시작 메뉴에서 "SimpleSerialToApi" 검색
2. 애플리케이션 실행
3. 메인 창이 정상적으로 표시되는지 확인

## 4. 초기 설정

### 4.1 Serial 포트 설정
1. **Settings** 메뉴 선택
2. **Serial Configuration** 탭 이동
3. 설정 항목:
   - **Port Name**: COM1, COM2 등 (장치 관리자에서 확인)
   - **Baud Rate**: 9600, 19200, 38400, 115200 등
   - **Data Bits**: 7 또는 8
   - **Parity**: None, Even, Odd
   - **Stop Bits**: 1 또는 2
   - **Handshake**: None, XOnXOff, RequestToSend
4. **Test Connection** 버튼으로 연결 테스트
5. **Save** 버튼으로 설정 저장

### 4.2 API 엔드포인트 구성
1. **Settings** 메뉴에서 **API Configuration** 탭 선택
2. 설정 항목:
   - **Base URL**: API 서버 주소 (예: https://api.example.com)
   - **Endpoint**: 데이터 전송 경로 (예: /sensors/data)
   - **Method**: POST, PUT 선택
   - **Content Type**: application/json
   - **Authentication Type**: None, Bearer Token, Basic Auth, API Key
   - **Authentication Value**: 인증 토큰 또는 키 입력
3. **Test API** 버튼으로 연결 테스트
4. **Save** 버튼으로 설정 저장

### 4.3 데이터 매핑 규칙 설정
1. **Settings** 메뉴에서 **Data Mapping** 탭 선택
2. **Add Mapping Rule** 버튼 클릭
3. 매핑 규칙 설정:
   - **Device Type**: 장비 유형 선택
   - **Data Pattern**: 수신 데이터 패턴 (정규표현식)
   - **JSON Template**: API 전송용 JSON 템플릿
   - **Field Mapping**: 데이터 필드 매핑 규칙
4. **Validate** 버튼으로 매핑 테스트
5. **Save** 버튼으로 규칙 저장

### 4.4 로그 설정
1. **Settings** 메뉴에서 **Logging** 탭 선택
2. 로그 레벨 설정:
   - **Debug**: 상세한 디버그 정보
   - **Information**: 일반적인 작업 정보
   - **Warning**: 경고 메시지
   - **Error**: 오류 정보만
3. 로그 출력 설정:
   - **File Logging**: 파일로 저장 여부
   - **Log File Path**: 로그 파일 저장 경로
   - **Max File Size**: 최대 파일 크기
   - **File Rotation**: 파일 순환 정책
4. **Apply** 버튼으로 설정 적용

## 5. 사용법

### 5.1 애플리케이션 시작
1. 바탕화면 바로가기 또는 시작 메뉴에서 실행
2. 메인 창에서 현재 설정 상태 확인
3. Serial 장비와 케이블 연결 확인
4. **Connect** 버튼 클릭하여 Serial 연결 시작

### 5.2 연결 상태 확인
- **Status Bar**: 하단 상태바에서 연결 상태 표시
  - 🟢 녹색: 정상 연결
  - 🟡 노란색: 연결 중
  - 🔴 빨간색: 연결 실패
- **Connection Info**: 우측 패널에서 상세 연결 정보 확인
- **Statistics**: 송수신 데이터 통계 정보

### 5.3 데이터 전송 모니터링
1. **Data Monitor** 탭에서 실시간 데이터 확인
2. **Raw Data**: Serial로 수신된 원본 데이터
3. **Parsed Data**: 파싱된 데이터 내용
4. **API Payload**: API로 전송될 JSON 데이터
5. **Response**: API 서버 응답 메시지
6. **Queue Status**: 메시지 큐 처리 상태

### 5.4 로그 확인
1. **Log Viewer** 탭에서 로그 메시지 확인
2. 로그 레벨별 필터링 기능
3. **Clear Log** 버튼으로 화면 로그 초기화
4. **Export Log** 버튼으로 로그 파일 내보내기

### 5.5 수동 데이터 전송
1. **Manual Control** 탭 이용
2. **Raw Data Input**: 테스트용 데이터 입력
3. **Send to API** 버튼으로 수동 전송
4. **Response Preview**: 전송 결과 확인

## 6. 오류 대응 방법

### 6.1 Serial 연결 오류
**문제**: "COM 포트를 찾을 수 없습니다"
- **원인**: COM 포트가 존재하지 않거나 다른 프로그램에서 사용 중
- **해결방법**:
  1. 장치 관리자에서 포트 상태 확인
  2. 다른 프로그램 종료 후 재시도
  3. USB-to-Serial 드라이버 재설치

**문제**: "Serial 통신 타임아웃"
- **원인**: 장비 응답 없음 또는 설정 불일치
- **해결방법**:
  1. 장비 전원 및 케이블 연결 확인
  2. Baud Rate, Data Bits 설정 확인
  3. 장비별 통신 규격 매뉴얼 참조

### 6.2 API 전송 오류
**문제**: "HTTP 401 Unauthorized"
- **원인**: 인증 정보 오류
- **해결방법**:
  1. Authentication Type 설정 확인
  2. Token 또는 Key 값 재확인
  3. API 서버 관리자에게 권한 확인 요청

**문제**: "HTTP 500 Internal Server Error"
- **원인**: API 서버 내부 오류
- **해결방법**:
  1. JSON 데이터 형식 확인
  2. API 서버 상태 확인
  3. 서버 로그 확인 또는 관리자 문의

### 6.3 데이터 파싱 오류
**문제**: "데이터 형식을 인식할 수 없습니다"
- **원인**: 매핑 규칙과 실제 데이터 불일치
- **해결방법**:
  1. Raw Data Monitor에서 실제 수신 데이터 확인
  2. Data Mapping 설정에서 패턴 수정
  3. Test Mapping 기능으로 검증

### 6.4 큐 처리 오류
**문제**: "메시지 큐가 가득 참"
- **원인**: 처리 속도보다 수신 속도가 빠름
- **해결방법**:
  1. Queue Size 설정 증가
  2. Batch Size 조정으로 처리 효율 향상
  3. 불필요한 데이터 필터링 설정

## 7. 고급 기능

### 7.1 배치 처리 설정
- 여러 데이터를 묶어서 한 번에 API 전송
- 네트워크 효율성 향상 및 서버 부하 감소
- Batch Size와 Timeout 설정으로 조절

### 7.2 재시도 정책 설정
- API 전송 실패 시 자동 재시도
- 지수 백오프를 통한 부하 조절
- 최대 재시도 횟수 설정

### 7.3 데이터 필터링
- 중요하지 않은 데이터 제외
- 조건부 전송 규칙 설정
- 데이터 검증 규칙 적용

### 7.4 알림 설정
- 오류 발생 시 이메일 알림
- 시스템 트레이 알림
- 로그 파일 크기 제한 알림

## 8. 문제해결 도구

### 8.1 진단 도구
- **Connection Test**: 연결 상태 진단
- **Data Flow Test**: 데이터 흐름 추적
- **Performance Monitor**: 성능 모니터링

### 8.2 로그 분석
- 로그 패턴 분석 기능
- 오류 발생 빈도 통계
- 성능 지표 추이 분석

## 9. 성능 최적화

### 9.1 메모리 사용량 최적화
- 큐 크기 적절히 설정 (기본: 1000개)
- 로그 레벨을 Information 이상으로 설정
- 불필요한 데이터 수집 비활성화

### 9.2 네트워크 최적화
- Connection Timeout 적절히 설정
- Keep-Alive 연결 사용
- 압축 전송 활성화 (지원 시)

## 10. 백업 및 복원

### 10.1 설정 백업
- **File** → **Export Settings** 메뉴 사용
- 모든 설정이 JSON 파일로 저장됨
- 정기적인 설정 백업 권장

### 10.2 설정 복원
- **File** → **Import Settings** 메뉴 사용
- 이전에 백업된 JSON 파일 선택
- 애플리케이션 재시작 필요

## 11. 기술 지원

### 11.1 로그 수집
문제 발생 시 다음 로그 파일을 기술지원팀에 제공:
- Application 로그: `Logs/SimpleSerialToApi.log`
- System 로그: Windows 이벤트 뷰어
- Configuration: 현재 설정 파일 (App.config)

### 11.2 연락처
- 이메일: support@yourcompany.com
- 전화: +82-2-xxxx-xxxx
- 온라인 지원: https://support.yourcompany.com

## 12. 라이선스 및 저작권
이 소프트웨어는 MIT 라이선스에 따라 배포됩니다. 자세한 내용은 LICENSE 파일을 참조하십시오.