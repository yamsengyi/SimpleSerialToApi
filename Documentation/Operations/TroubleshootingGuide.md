# SimpleSerialToApi 장애 대응 매뉴얼

## 1. 긴급 대응 프로세스

### 1.1 장애 심각도 분류
- **Critical (P1)**: 서비스 완전 중단, 데이터 손실 위험
- **High (P2)**: 주요 기능 장애, 성능 심각 저하
- **Medium (P3)**: 일부 기능 장애, 회피 방법 존재
- **Low (P4)**: 사소한 문제, 사용자 편의성 영향

### 1.2 대응 시간 목표 (SLA)
- **P1**: 15분 이내 초기 대응, 1시간 이내 복구
- **P2**: 30분 이내 초기 대응, 4시간 이내 복구
- **P3**: 2시간 이내 대응, 1일 이내 복구
- **P4**: 1일 이내 대응, 1주일 이내 해결

### 1.3 장애 대응 팀 연락처
- **1차 대응자**: 기술지원팀 (+82-2-xxxx-1111)
- **2차 대응자**: 개발팀 (+82-2-xxxx-2222)
- **3차 대응자**: 시스템 관리자 (+82-2-xxxx-3333)
- **긴급 상황**: 24시간 핫라인 (+82-2-xxxx-9999)

## 2. 일반적인 장애 유형 및 해결방법

### 2.1 Serial 연결 장애

#### 장애 증상
- COM 포트 연결 실패
- "포트를 찾을 수 없습니다" 오류
- 데이터 수신 중단
- 연결 타임아웃 발생

#### 진단 단계
```powershell
# 1. 사용 가능한 COM 포트 확인
Get-WmiObject -Class Win32_SerialPort | Select-Object Name, Status, DeviceID

# 2. 포트 사용 중인 프로세스 확인
Get-Process | Where-Object {$_.ProcessName -like "*serial*"}

# 3. 디바이스 관리자에서 포트 상태 확인
devmgmt.msc
```

#### 해결 방법
**즉시 조치 (5분 이내)**
1. 애플리케이션 재시작
   ```cmd
   taskkill /f /im SimpleSerialToApi.exe
   start "" "C:\Program Files\SimpleSerialToApi\SimpleSerialToApi.exe"
   ```

2. Serial 케이블 연결 확인
   - 케이블 연결 상태 점검
   - 다른 케이블로 교체 테스트

3. USB-to-Serial 드라이버 재설치
   - 장치 관리자에서 드라이버 제거 후 재설치
   - 제조사 최신 드라이버 다운로드

**단계별 복구 (30분 이내)**
1. **포트 충돌 해결**
   ```powershell
   # 포트 사용 중인 프로세스 종료
   Get-Process -Name "*터미널*", "*시리얼*" | Stop-Process -Force
   ```

2. **다른 COM 포트로 변경**
   - App.config에서 포트 번호 변경
   - 장비 설정에서 포트 매핑 확인

3. **권한 문제 해결**
   - 관리자 권한으로 애플리케이션 실행
   - 사용자 계정 권한 확인

#### 예방 조치
- 정기적인 케이블 교체 (6개월마다)
- 드라이버 자동 업데이트 설정
- 백업 Serial 포트 준비

### 2.2 API 통신 장애

#### 장애 증상
- HTTP 오류 응답 (4xx, 5xx)
- 연결 타임아웃
- 인증 실패 (401 Unauthorized)
- 데이터 전송 실패

#### 진단 단계
```powershell
# 1. 네트워크 연결 테스트
Test-NetConnection api.example.com -Port 443

# 2. DNS 해상도 확인
nslookup api.example.com

# 3. HTTP 응답 테스트
curl -I https://api.example.com/health

# 4. 인증 토큰 유효성 확인
curl -H "Authorization: Bearer TOKEN" https://api.example.com/auth/verify
```

#### 해결 방법
**즉시 조치 (5분 이내)**
1. **백업 API 엔드포인트로 전환**
   ```xml
   <!-- App.config 수정 -->
   <add key="API.BaseUrl" value="https://backup-api.example.com" />
   ```

2. **인증 토큰 갱신**
   - 새로운 Bearer 토큰 발급
   - App.config 업데이트 후 재시작

3. **재시도 정책 조정**
   ```xml
   <add key="API.Retry.MaxAttempts" value="5" />
   <add key="API.Retry.InitialDelay" value="2000" />
   ```

**단계별 복구 (30분 이내)**
1. **네트워크 문제 해결**
   ```cmd
   # DNS 캐시 초기화
   ipconfig /flushdns
   
   # 네트워크 어댑터 재시작
   netsh winsock reset
   ```

2. **프록시 설정 확인**
   - 회사 방화벽/프록시 설정 검토
   - 프록시 인증 정보 업데이트

3. **API 서버 상태 확인**
   - 서버 관리팀에 상태 확인 요청
   - 대체 API 엔드포인트 준비

#### 예방 조치
- 다중 API 엔드포인트 설정
- 인증 토큰 자동 갱신 시스템
- 네트워크 모니터링 도구 설치

### 2.3 메모리 부족 문제

#### 장애 증상
- OutOfMemoryException 발생
- 애플리케이션 응답 지연
- 시스템 전체 성능 저하
- 로그에 GC 관련 경고

#### 진단 단계
```powershell
# 1. 메모리 사용량 확인
Get-Process -Name "SimpleSerialToApi" | Select-Object Name, WorkingSet64, PagedMemorySize64

# 2. 시스템 메모리 상태
Get-WmiObject -Class Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory

# 3. .NET 메모리 카운터 확인
Get-Counter "\\.NET CLR Memory(SimpleSerialToApi)\# Bytes in all Heaps"
```

#### 해결 방법
**즉시 조치 (5분 이내)**
1. **애플리케이션 재시작**
   - 메모리 해제를 위한 강제 재시작
   - 프로세스 종료 후 새로 시작

2. **로그 레벨 조정**
   ```xml
   <!-- 로그 양 감소 -->
   <add key="Serilog.MinimumLevel" value="Warning" />
   ```

3. **큐 크기 축소**
   ```xml
   <add key="Queue.MaxCapacity" value="500" />
   <add key="Queue.BatchSize" value="25" />
   ```

**단계별 복구 (30분 이내)**
1. **메모리 누수 확인**
   - Process Explorer로 메모리 사용량 모니터링
   - 메모리 덤프 생성 후 분석

2. **GC 튜닝 적용**
   ```xml
   <runtime>
     <gcServer enabled="true"/>
     <gcConcurrent enabled="true"/>
   </runtime>
   ```

3. **데이터 보관 기간 단축**
   ```xml
   <add key="Data.RetentionDays" value="7" />
   <add key="Log.MaxFiles" value="5" />
   ```

#### 예방 조치
- 정기적인 메모리 사용량 모니터링
- 자동 재시작 스케줄 설정
- 메모리 임계값 알림 설정

### 2.4 큐 오버플로우 문제

#### 장애 증상
- "Queue is full" 오류 메시지
- 데이터 처리 지연 증가
- 메시지 손실 발생
- API 응답 지연

#### 진단 단계
```csharp
// 애플리케이션 로그에서 큐 상태 확인
// Queue depth: 950/1000 (95%)
// Processing rate: 45 msg/sec
// Error rate: 5%
```

#### 해결 방법
**즉시 조치 (5분 이내)**
1. **큐 크기 증가**
   ```xml
   <add key="Queue.MaxCapacity" value="2000" />
   ```

2. **배치 크기 증가**
   ```xml
   <add key="Queue.BatchSize" value="100" />
   <add key="Queue.BatchTimeout" value="3000" />
   ```

3. **처리 스레드 증가**
   ```xml
   <add key="Queue.ProcessingThreads" value="4" />
   ```

**단계별 복구 (30분 이내)**
1. **불필요한 데이터 필터링**
   - 중복 데이터 제거 규칙 적용
   - 중요도가 낮은 데이터 제외

2. **API 호출 최적화**
   - 병렬 API 호출 증가
   - 연결 풀 크기 조정

3. **백압력 제어 구현**
   - Serial 데이터 수신 속도 조절
   - 임시 데이터 저장 활용

#### 예방 조치
- 큐 깊이 모니터링 및 알림
- 자동 스케일링 정책 수립
- 피크 시간대 처리 능력 확보

## 3. 장애 대응 체크리스트

### 3.1 초기 대응 체크리스트 (5분 이내)
- [ ] 장애 신고 접수 및 심각도 분류
- [ ] 1차 대응자 배정
- [ ] 기본 상태 확인 (프로세스, 연결, 로그)
- [ ] 임시 해결책 적용 (재시작, 설정 변경)
- [ ] 관련자에게 장애 상황 공유

### 3.2 상세 진단 체크리스트 (15분 이내)
- [ ] 로그 파일 분석
- [ ] 시스템 리소스 상태 확인
- [ ] 네트워크 연결 상태 점검
- [ ] 외부 서비스 의존성 확인
- [ ] 최근 변경사항 검토

### 3.3 복구 실행 체크리스트 (30분 이내)
- [ ] 근본 원인 파악
- [ ] 복구 계획 수립
- [ ] 백업 시스템으로 전환 (필요시)
- [ ] 복구 작업 실행
- [ ] 복구 상태 검증

### 3.4 사후 처리 체크리스트 (2시간 이내)
- [ ] 완전 복구 확인
- [ ] 서비스 정상화 공지
- [ ] 장애 보고서 작성
- [ ] 재발 방지 대책 수립
- [ ] 모니터링 개선사항 적용

## 4. 장애 상황별 대응 매트릭스

| 장애 유형 | 심각도 | 초기 대응 | 복구 시간 | 담당자 |
|-----------|--------|-----------|-----------|---------|
| Serial 연결 실패 | P2 | 애플리케이션 재시작 | 30분 | 기술지원팀 |
| API 인증 오류 | P2 | 토큰 갱신 | 15분 | 개발팀 |
| 메모리 부족 | P1 | 즉시 재시작 | 15분 | 시스템관리자 |
| 큐 오버플로우 | P2 | 설정 조정 | 30분 | 개발팀 |
| 네트워크 단절 | P1 | 네트워크 확인 | 60분 | 네트워크팀 |
| 설정 파일 손상 | P2 | 백업 복원 | 20분 | 기술지원팀 |

## 5. 장애 예방 모니터링

### 5.1 주요 모니터링 지표
- **System Metrics**
  - CPU 사용률 (임계값: 80%)
  - 메모리 사용률 (임계값: 85%)
  - 디스크 공간 (임계값: 90%)

- **Application Metrics**
  - Serial 연결 상태
  - API 응답시간 (임계값: 5초)
  - 큐 깊이 (임계값: 80%)
  - 오류율 (임계값: 5%)

- **Business Metrics**
  - 데이터 처리량
  - 데이터 손실률
  - 서비스 가용률 (목표: 99.9%)

### 5.2 알림 설정
```xml
<Monitoring>
  <Alerts>
    <Alert name="SerialDisconnected" 
           condition="serial_connected = false" 
           action="email,sms" 
           recipients="support@company.com" />
    
    <Alert name="HighMemoryUsage" 
           condition="memory_usage > 85%" 
           action="email" 
           recipients="admin@company.com" />
    
    <Alert name="QueueOverflow" 
           condition="queue_depth > 80%" 
           action="email" 
           recipients="dev@company.com" />
  </Alerts>
</Monitoring>
```

## 6. 긴급 연락망

### 6.1 내부 연락망
- **기술지원팀장**: 010-1234-5678
- **개발팀장**: 010-2345-6789
- **시스템관리자**: 010-3456-7890
- **프로젝트매니저**: 010-4567-8901

### 6.2 외부 업체 연락망
- **Serial 장비 업체**: 1588-1234
- **API 서비스 업체**: 1588-5678
- **네트워크 서비스**: 1588-9012

## 7. 장애 보고서 양식

### 7.1 장애 기본 정보
- **장애 발생 시간**: 
- **장애 발견 방법**: 
- **영향 범위**: 
- **심각도**: 

### 7.2 장애 상세 내용
- **장애 증상**: 
- **추정 원인**: 
- **적용된 임시 조치**: 
- **최종 해결 방법**: 

### 7.3 재발 방지 대책
- **근본 원인 분석**: 
- **개선 계획**: 
- **모니터링 강화**: 
- **교육 계획**: 

이 장애 대응 매뉴얼을 정기적으로 검토하고 업데이트하여 최신 상태를 유지해야 합니다.