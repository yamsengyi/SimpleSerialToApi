# Step 04: 데이터 파싱 및 매핑 프레임워크

## 개요
Serial 포트에서 수신된 raw 데이터를 파싱하고, App.Config의 매핑 규칙에 따라 API 전송용 데이터 구조로 변환하는 프레임워크를 구현합니다.

## 상세 작업

### 4.1 데이터 파서 아키텍처 설계
- `IDataParser` 인터페이스 정의
- `DataParserFactory`: 데이터 포맷별 파서 생성
- 플러그인 방식으로 신규 파서 추가 지원
- HEX, TEXT, JSON, Binary 파서 구현

### 4.2 데이터 매핑 엔진
- `IDataMappingEngine` 인터페이스
- `DataMappingEngine` 구현 클래스
- App.Config 매핑 규칙 적용
- 데이터 타입 변환 및 검증

### 4.3 데이터 변환기 시스템
- `IDataConverter` 인터페이스
- 기본 변환기들 구현:
  - `TemperatureConverter`: 온도 단위 변환
  - `DateTimeConverter`: 시간 형식 변환
  - `NumericConverter`: 숫자 형식 변환
  - `StringConverter`: 문자열 형식 변환
- 커스텀 변환기 등록 메커니즘

### 4.4 파싱된 데이터 모델
```csharp
// 원시 수신 데이터
public class RawSerialData
{
    public DateTime ReceivedTime { get; set; }
    public byte[] Data { get; set; }
    public string DataFormat { get; set; }
    public string DeviceId { get; set; }
}

// 파싱된 데이터
public class ParsedData
{
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; }
    public Dictionary<string, object> Fields { get; set; }
    public string DataSource { get; set; }
}

// API 전송용 매핑된 데이터
public class MappedApiData
{
    public string EndpointName { get; set; }
    public Dictionary<string, object> Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MessageId { get; set; }
}
```

### 4.5 파싱 규칙 설정
```xml
<!-- App.config의 파싱 규칙 섹션 -->
<parsingRules>
  <rules>
    <add name="TemperatureSensor"
         pattern="TEMP:([0-9.]+)C;HUMID:([0-9.]+)%"
         fields="temperature,humidity"
         dataTypes="decimal,decimal" />
    <add name="PressureSensor"
         pattern="PRESS:([0-9.]+)hPa"
         fields="pressure"
         dataTypes="decimal" />
  </rules>
  <converters>
    <add name="TemperatureConverter"
         sourceUnit="celsius"
         targetUnit="fahrenheit"
         formula="(x * 9/5) + 32" />
  </converters>
</parsingRules>
```

### 4.6 매핑 엔진 구현
- 정규식 기반 패턴 매칭
- JSON Path를 활용한 계층적 데이터 매핑
- 조건부 매핑 (if-then 규칙)
- 집계 및 계산 필드 지원

## 기술 요구사항
- System.Text.RegularExpressions 활용
- Newtonsoft.Json for JSON 파싱
- 플러그인 아키텍처 (MEF 또는 DI)
- 성능 최적화 (캐싱, 컴파일된 정규식)

## 주요 클래스 및 인터페이스

### IDataParser
```csharp
public interface IDataParser
{
    string SupportedFormat { get; }
    ParsedData Parse(RawSerialData rawData, ParsingRule rule);
    bool CanParse(RawSerialData rawData);
    ValidationResult ValidateRule(ParsingRule rule);
}
```

### IDataMappingEngine
```csharp
public interface IDataMappingEngine
{
    MappedApiData MapToApiData(ParsedData parsedData, string endpointName);
    Task<List<MappedApiData>> MapBatchAsync(List<ParsedData> parsedDataList);
    void RegisterConverter(string name, IDataConverter converter);
    ValidationResult ValidateMappingRule(MappingRuleConfig rule);
}
```

### IDataConverter
```csharp
public interface IDataConverter
{
    string Name { get; }
    object Convert(object input, ConversionContext context);
    bool CanConvert(Type sourceType, Type targetType);
    Type[] SupportedTypes { get; }
}
```

## 산출물
- [x] `IDataParser` 인터페이스 및 구현체들
- [x] `IDataMappingEngine` 인터페이스 및 구현체
- [x] `IDataConverter` 인터페이스 및 기본 변환기들
- [x] 데이터 모델 클래스들 (RawSerialData, ParsedData, MappedApiData)
- [x] `DataParserFactory` 클래스
- [x] 파싱 및 매핑 규칙 설정 섹션
- [x] 파싱 성능 모니터링 유틸리티
- [x] 데이터 파싱/매핑 단위 테스트

## 완료 조건
1. 다양한 형식의 Serial 데이터를 정상적으로 파싱할 수 있음
2. App.Config 매핑 규칙에 따라 데이터 변환이 정확히 수행됨
3. 커스텀 데이터 변환기를 등록하고 사용할 수 있음
4. 파싱 실패 시 적절한 오류 처리 및 로그 기록
5. 대용량 데이터 처리 시 성능 기준 충족 (1초 이내)
6. 잘못된 파싱/매핑 규칙에 대한 검증 기능 동작
7. 모든 파서 및 변환기에 대한 단위 테스트 통과

## 다음 단계 의존성
이 단계가 완료되어야 Step 05 (Message Queue 구현)를 진행할 수 있습니다.

## 예상 소요 시간
**3-4일 (24-32시간)**

## 성능 목표
- 단일 메시지 파싱: < 50ms
- 배치 매핑 (100건): < 500ms
- 메모리 사용량: < 100MB (1000건 처리 시)

## 주의사항
- 정규식 성능 최적화 (컴파일된 정규식 사용)
- 매핑 규칙 순환 참조 방지
- 대용량 데이터 처리 시 메모리 관리
- 스레드 안전성 고려 (불변 객체 활용)

## 담당자 역할
- **개발자**: 파서 및 매핑 엔진 구현
- **데이터 분석가**: 매핑 규칙 및 변환 로직 설계
- **성능 엔지니어**: 파싱 성능 최적화
- **검토자**: 아키텍처 및 성능 검토