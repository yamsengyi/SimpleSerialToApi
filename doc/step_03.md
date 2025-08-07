# Step 03: 설정 관리 (App.Config) 시스템

## 개요
App.Config 기반의 포괄적인 설정 관리 시스템을 구축하고, API 매핑 규칙 및 애플리케이션 설정을 동적으로 관리할 수 있도록 구현합니다.

## 상세 작업

### 3.1 설정 관리 서비스 설계
- `IConfigurationService` 인터페이스 정의
- `ConfigurationService` 구현 클래스
- 설정 변경 감지 및 실시간 반영 기능
- 설정 값 유효성 검사

### 3.2 App.Config 구조 설계
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <!-- Serial 통신 설정 -->
  <appSettings>
    <add key="SerialPort" value="COM3" />
    <add key="BaudRate" value="9600" />
    <add key="Parity" value="None" />
    <add key="DataBits" value="8" />
    <add key="StopBits" value="One" />
  </appSettings>
  
  <!-- API 매핑 설정 -->
  <configSections>
    <section name="apiMappings" type="SimpleSerialToApi.Configuration.ApiMappingSection, SimpleSerialToApi" />
    <section name="messageQueue" type="SimpleSerialToApi.Configuration.MessageQueueSection, SimpleSerialToApi" />
  </configSections>
  
  <apiMappings>
    <endpoints>
      <add name="SensorDataEndpoint" 
           url="https://api.example.com/sensor-data" 
           method="POST" 
           authType="Bearer"
           timeout="30000" />
    </endpoints>
    <mappingRules>
      <add sourceField="temperature" 
           targetField="temp_celsius" 
           dataType="decimal" 
           converter="TemperatureConverter" />
    </mappingRules>
  </apiMappings>
  
  <messageQueue>
    <add key="MaxQueueSize" value="1000" />
    <add key="BatchSize" value="10" />
    <add key="RetryCount" value="3" />
    <add key="RetryInterval" value="5000" />
  </messageQueue>
</configuration>
```

### 3.3 설정 모델 클래스 생성
- `ApiEndpointConfig`: API 엔드포인트 설정
- `MappingRuleConfig`: 데이터 매핑 규칙
- `MessageQueueConfig`: 메시지 큐 설정
- `ApplicationConfig`: 전체 애플리케이션 설정

### 3.4 커스텀 Configuration Section 구현
- `ApiMappingSection`: API 매핑 설정 섹션
- `MessageQueueSection`: 메시지 큐 설정 섹션
- XML 스키마 검증 및 오류 처리

### 3.5 설정 변경 감지 및 Hot Reload
- FileSystemWatcher를 활용한 Config 파일 변경 감지
- 설정 변경 시 관련 서비스 자동 재시작
- 변경 사항 로그 기록

## 기술 요구사항
- System.Configuration 활용
- Microsoft.Extensions.Configuration 통합
- 설정 값 암호화 (민감 정보)
- 설정 검증 및 오류 처리

## 주요 클래스 및 인터페이스

### IConfigurationService
```csharp
public interface IConfigurationService
{
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    
    T GetSection<T>(string sectionName) where T : class, new();
    string GetAppSetting(string key);
    void ReloadConfiguration();
    bool ValidateConfiguration();
    void EncryptSection(string sectionName);
    void DecryptSection(string sectionName);
}
```

### ApiEndpointConfig
```csharp
public class ApiEndpointConfig
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Method { get; set; }
    public string AuthType { get; set; }
    public string AuthToken { get; set; }
    public int Timeout { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}
```

### MappingRuleConfig
```csharp
public class MappingRuleConfig
{
    public string SourceField { get; set; }
    public string TargetField { get; set; }
    public string DataType { get; set; }
    public string Converter { get; set; }
    public string DefaultValue { get; set; }
    public bool IsRequired { get; set; }
}
```

## 산출물
- [x] `IConfigurationService` 인터페이스
- [x] `ConfigurationService` 구현 클래스
- [x] 설정 모델 클래스들 (ApiEndpointConfig, MappingRuleConfig 등)
- [x] 커스텀 Configuration Section 클래스들
- [x] App.config 템플릿 파일
- [x] 설정 유효성 검사기
- [x] 설정 암호화 유틸리티
- [x] Configuration 관련 단위 테스트

## 완료 조건
1. App.config에서 모든 설정을 정상적으로 읽어올 수 있음
2. 설정 변경 시 실시간으로 감지하여 반영됨
3. 잘못된 설정값에 대한 유효성 검사가 동작함
4. 민감정보 (인증토큰 등)가 암호화되어 저장됨
5. API 매핑 규칙이 올바르게 파싱됨
6. 설정 변경 이벤트가 정상적으로 발생함
7. 모든 기능에 대한 단위 테스트가 통과함

## 다음 단계 의존성
이 단계가 완료되어야 Step 04 (데이터 파싱 및 매핑)를 진행할 수 있습니다.

## 예상 소요 시간
**2-3일 (16-24시간)**

## 주의사항
- 설정 파일 변경 감지 시 무한 루프 방지
- 암호화된 설정의 복호화 성능 고려
- 멀티스레드 환경에서의 설정 접근 동시성 처리
- 설정 검증 실패 시 애플리케이션 동작 방식 정의

## 담당자 역할
- **개발자**: Configuration 시스템 구현, 설정 모델 설계
- **보안 담당자**: 민감정보 암호화 방식 검토
- **검토자**: 설정 구조 및 보안 검토