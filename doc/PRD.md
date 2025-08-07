# 제품 요구 사항 명세서 (PRD)  
## 프로젝트명: SimpleSerialToApi (dotnet 8 기반)

---

### 1. 개요
Windows 환경에서 Serial 통신으로 장비 데이터를 수집한 뒤, 지정 포맷에 따라 연동 API로 전송하는 로컬 애플리케이션을 개발한다. 내부 Message Queue와 App.Config 기반의 API 매핑 기능을 포함한다.

---

### 2. 주요 기능

#### 2.1 Serial 통신 및 장비 초기화
- 사용자가 지정한 시리얼 포트(예: COM3, COM4 등)로 장비와 연결
- App.Config 또는 별도 UI를 통해 시리얼 포트 설정(baudrate, parity, stopbits 등) 지원
- 장비 초기화 명령을 지정 포맷으로 송신 (예: HEX, TEXT 등)
- 초기화 결과(ACK, NACK 등) 처리 및 상태 표시

#### 2.2 장비와의 데이터 매핑 및 송수신
- 실시간으로 Serial 데이터를 읽어 지정 포맷으로 변환
- 수신 데이터에서 필요한 정보 파싱 및 매핑 규칙에 따라 API 전송 데이터 구조화
- 장비와의 통신 로그 기록 및 오류 발생 시 사용자 알림

#### 2.3 API 연동 및 전송 관리
- App.Config에 등록된 API Endpoint, 메서드, 인증정보를 기반으로 연동
- 파싱된 데이터를 REST API(POST/PUT 등)로 전송
- API 응답 결과(성공/실패)를 기록 및 상태 관리

#### 2.4 내부 Message Queue
- Serial 수신 이벤트 발생 시 메시지를 내부 Queue에 적재
- Queue에 쌓인 데이터는 API 전송 순서/속도 제어를 위해 순차적으로 처리
- 메시지 처리 실패 시 재시도 정책 및 최대 재시도 횟수 지원

#### 2.5 App.Config 기반 API 매핑
- App.Config 파일에 API Endpoint, 파라미터, 매핑 규칙 등 설정
- 환경설정 변경 시 재시작 없이 반영(가능하다면)
- 설정값 유효성 검사 및 오류 로그 기록

---

### 3. 비기능 요구사항

#### 3.1 성능
- 1초 이내 Serial 데이터 파싱 및 API 전송
- 메시지 Queue는 1000건 이상 동시 처리 가능

#### 3.2 안정성
- 예외 및 오류 발생 시 복구 로직 내장
- Serial/Network 장애시 자동 재연결 및 재시도

#### 3.3 유지관리 및 확장성
- API Endpoint 및 매핑 규칙은 App.Config에서 손쉽게 추가/변경
- 신규 장비 포맷/프로토콜 추가 용이
- 코드 및 기능별 로그 기록

#### 3.4 보안
- API 인증 방식(토큰, Basic Auth 등) 지원
- 민감정보(App.Config 내 인증정보)는 암호화 저장 및 접근 제한

---

### 4. UI 요구사항
- Serial 연결상태, API 전송상태 실시간 표시
- 설정값(App.Config)을 통한 초기화 기능
- 로그 및 오류 내역 조회 기능

---

### 5. 기술 요구사항
- dotnet 8 기반 WPF (C#)
- System.IO.Ports, HttpClient, ConfigurationManager 등 표준 라이브러리 활용
- 단위 테스트 프로젝트 포함

---

### 6. 기타
- 상세 설계 및 구현시 추가 요구사항 반영 가능
- 필요시 개발/운영 매뉴얼 제공

---

**작성자:** yamsengyi  
**작성일:** 2025-08-07
