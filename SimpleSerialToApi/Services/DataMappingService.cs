using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 데이터 매핑 서비스 - 시나리오 기반으로 데이터를 처리하고 적절한 형태로 전송
    /// </summary>
    public class DataMappingService
    {
        private readonly ILogger<DataMappingService> _logger;
        private readonly ReservedWordService _reservedWordService;
        private readonly List<DataMappingScenario> _scenarios;

        /// <summary>
        /// 매핑 처리 완료 이벤트
        /// </summary>
        public event EventHandler<MappingProcessedEventArgs>? MappingProcessed;

        public DataMappingService(
            ILogger<DataMappingService> logger,
            ReservedWordService reservedWordService)
        {
            _logger = logger;
            _reservedWordService = reservedWordService;
            _scenarios = new List<DataMappingScenario>();

            // 기본 시나리오들 로드
            LoadDefaultScenarios();
        }

        /// <summary>
        /// 현재 등록된 매핑 시나리오 목록
        /// </summary>
        public IReadOnlyList<DataMappingScenario> Scenarios => _scenarios.AsReadOnly();

        /// <summary>
        /// 매핑 시나리오 추가
        /// </summary>
        /// <param name="scenario">추가할 시나리오</param>
        public void AddScenario(DataMappingScenario scenario)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));

            if (_scenarios.Count >= 10)
            {
                _logger.LogWarning("Maximum number of scenarios (10) reached. Cannot add more scenarios.");
                return;
            }

            _scenarios.Add(scenario);
            _logger.LogInformation("Added mapping scenario: {Name}", scenario.Name);
        }

        /// <summary>
        /// 매핑 시나리오 제거
        /// </summary>
        /// <param name="scenario">제거할 시나리오</param>
        public bool RemoveScenario(DataMappingScenario scenario)
        {
            var removed = _scenarios.Remove(scenario);
            if (removed)
            {
                _logger.LogInformation("Removed mapping scenario: {Name}", scenario.Name);
            }
            return removed;
        }

        /// <summary>
        /// 시나리오 목록 초기화
        /// </summary>
        public void ClearScenarios()
        {
            _scenarios.Clear();
            _logger.LogInformation("All mapping scenarios cleared");
        }

        /// <summary>
        /// 입력 데이터를 시나리오에 따라 처리
        /// </summary>
        /// <param name="data">입력 데이터</param>
        /// <param name="source">데이터 소스</param>
        /// <returns>처리된 결과들</returns>
        public async Task<List<MappingResult>> ProcessDataAsync(string data, DataSource source)
        {
            var results = new List<MappingResult>();

            if (string.IsNullOrEmpty(data))
            {
                return results;
            }

            try
            {
                // 활성화된 시나리오들 중에서 해당 소스와 매칭되는 것들 찾기
                var matchingScenarios = _scenarios
                    .Where(s => s.IsEnabled && s.Source == source)
                    .ToList();

                _logger.LogDebug("Processing data from {Source}: '{Data}' with {Count} scenarios", 
                    source, data, matchingScenarios.Count);

                foreach (var scenario in matchingScenarios)
                {
                    if (await IsDataMatchingScenario(data, scenario))
                    {
                        var result = await ProcessScenario(data, scenario);
                        results.Add(result);
                        
                        // 매핑 처리 이벤트 발생
                        MappingProcessed?.Invoke(this, new MappingProcessedEventArgs
                        {
                            OriginalData = data,
                            Scenario = scenario,
                            Result = result
                        });
                    }
                }

                _logger.LogInformation("Processed data resulted in {Count} matches", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data: {Data}", data);
            }

            return results;
        }

        /// <summary>
        /// 데이터가 시나리오와 매칭되는지 확인
        /// </summary>
        /// <param name="data">확인할 데이터</param>
        /// <param name="scenario">시나리오</param>
        /// <returns>매칭 여부</returns>
        private async Task<bool> IsDataMatchingScenario(string data, DataMappingScenario scenario)
        {
            try
            {
                if (string.IsNullOrEmpty(scenario.Identifier))
                    return false;

                // indexOf를 사용한 저수준 매칭
                bool matches = data.IndexOf(scenario.Identifier, StringComparison.OrdinalIgnoreCase) != -1;
                
                if (matches)
                {
                    _logger.LogDebug("Data matched scenario '{Name}' with identifier '{Identifier}'", 
                        scenario.Name, scenario.Identifier);
                }

                return matches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking data match for scenario: {Name}", scenario.Name);
                return false;
            }
        }

        /// <summary>
        /// 시나리오에 따라 데이터 처리
        /// </summary>
        /// <param name="data">원본 데이터</param>
        /// <param name="scenario">적용할 시나리오</param>
        /// <returns>처리 결과</returns>
        private async Task<MappingResult> ProcessScenario(string data, DataMappingScenario scenario)
        {
            try
            {
                // 예약어 처리
                var processedValue = _reservedWordService.ProcessReservedWords(scenario.ValueTemplate);
                
                // 원본 데이터에서 유용한 정보 추출 (필요시)
                processedValue = await ExtractDataFromOriginal(data, processedValue, scenario);

                var result = new MappingResult
                {
                    Success = true,
                    ScenarioName = scenario.Name,
                    OriginalData = data,
                    ProcessedData = processedValue,
                    TransmissionType = scenario.TransmissionType,
                    ApiMethod = scenario.ApiMethod,
                    ApiEndpoint = scenario.ApiEndpoint,
                    Timestamp = DateTime.Now
                };

                _logger.LogInformation("Successfully processed scenario '{Name}': '{Original}' -> '{Processed}'", 
                    scenario.Name, data, processedValue);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scenario '{Name}'", scenario.Name);
                
                return new MappingResult
                {
                    Success = false,
                    ScenarioName = scenario.Name,
                    OriginalData = data,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 원본 데이터에서 값을 추출하여 템플릿에 적용
        /// </summary>
        /// <param name="originalData">원본 데이터</param>
        /// <param name="template">처리된 템플릿</param>
        /// <param name="scenario">시나리오</param>
        /// <returns>최종 처리된 데이터</returns>
        private async Task<string> ExtractDataFromOriginal(string originalData, string template, DataMappingScenario scenario)
        {
            // 여기서 원본 데이터에서 특정 값을 추출하여 템플릿에 적용하는 로직을 구현
            // 예: STX TEMP:25.6 ETX -> 25.6 추출
            
            // @originalData 예약어가 있으면 원본 데이터로 치환
            var result = template.Replace("@originalData", originalData);
            
            // 추가적인 데이터 추출 로직은 필요에 따라 구현
            // 예: 정규표현식을 사용한 값 추출 등
            
            return result;
        }

        /// <summary>
        /// 기본 시나리오들 로드
        /// </summary>
        private void LoadDefaultScenarios()
        {
            try
            {
                // 파일에서 시나리오 로드 시도
                if (LoadScenariosFromFile())
                {
                    _logger.LogInformation("Loaded {Count} scenarios from file", _scenarios.Count);
                    return;
                }

                // 파일 로드 실패 시 기본 시나리오들 생성
                var defaultScenarios = new[]
                {
                    new DataMappingScenario
                    {
                        Name = "Scenario 1",
                        Source = DataSource.Serial,
                        Identifier = "[01]",
                        ValueTemplate = "@yyyyMMddHHmmssfff-@deviceId-{value}",
                        TransmissionType = TransmissionType.Api,
                        ApiMethod = "POST",
                        ApiEndpoint = "/api/sensor-data",
                        IsEnabled = true
                    },
                    new DataMappingScenario
                    {
                        Name = "Scenario 2",
                        Source = DataSource.Serial,
                        Identifier = "[99]",
                        ValueTemplate = "{value}",
                        TransmissionType = TransmissionType.Api,
                        ApiMethod = "POST",
                        ApiEndpoint = "/api/test2.html",
                        IsEnabled = true
                    }
                };

                foreach (var scenario in defaultScenarios)
                {
                    _scenarios.Add(scenario);
                }

                // 기본 시나리오들을 파일에 저장
                SaveScenariosToFile();
                
                _logger.LogInformation("Loaded {Count} default scenarios", defaultScenarios.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading default scenarios");
            }
        }

        /// <summary>
        /// 시나리오들을 파일에 저장
        /// </summary>
        public void SaveScenariosToFile()
        {
            try
            {
                var scenariosFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data-mapping-scenarios.json");
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(_scenarios, options);
                File.WriteAllText(scenariosFilePath, json);
                
                _logger.LogInformation("Saved {Count} scenarios to file: {FilePath}", _scenarios.Count, scenariosFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scenarios to file");
            }
        }

        /// <summary>
        /// 파일에서 시나리오들을 로드
        /// </summary>
        private bool LoadScenariosFromFile()
        {
            try
            {
                var scenariosFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data-mapping-scenarios.json");
                
                if (!File.Exists(scenariosFilePath))
                {
                    return false;
                }

                var json = File.ReadAllText(scenariosFilePath);
                var scenarios = JsonSerializer.Deserialize<List<DataMappingScenario>>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (scenarios != null)
                {
                    _scenarios.Clear();
                    _scenarios.AddRange(scenarios);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scenarios from file");
            }
            
            return false;
        }
    }

    /// <summary>
    /// 매핑 결과 모델
    /// </summary>
    public class MappingResult
    {
        public bool Success { get; set; }
        public string ScenarioName { get; set; } = string.Empty;
        public string OriginalData { get; set; } = string.Empty;
        public string ProcessedData { get; set; } = string.Empty;
        public TransmissionType TransmissionType { get; set; }
        public string ApiMethod { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 매핑 처리 완료 이벤트 인자
    /// </summary>
    public class MappingProcessedEventArgs : EventArgs
    {
        public string OriginalData { get; set; } = string.Empty;
        public DataMappingScenario Scenario { get; set; } = null!;
        public MappingResult Result { get; set; } = null!;
    }
}
