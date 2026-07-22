using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 데이터 매핑 시나리오 관리를 위한 파사드 구현
    /// </summary>
    public class DataMappingFacade : IDataMappingFacade, INotifyPropertyChanged
    {
        private readonly ILogger<DataMappingFacade> _logger;
        private readonly DataMappingService _dataMappingService;
        private readonly ReservedWordService _reservedWordService;

        private DataMappingScenario? _selectedMappingScenario;

        public ObservableCollection<DataMappingScenario> MappingScenarios { get; } = new();
        public List<DataSource> DataSources { get; } = new() { DataSource.Serial, DataSource.ApiResponse };
        public List<TransmissionType> TransmissionTypes { get; } = new() { TransmissionType.Serial, TransmissionType.Api };
        public List<string> ApiMethods { get; } = new() { "GET", "POST", "PUT", "DELETE" };
        public List<string> ContentTypes { get; } = new()
        {
            "application/json", "application/xml", "text/plain", "text/html",
            "text/xml", "application/x-www-form-urlencoded", "multipart/form-data", "text/csv"
        };

        public DataMappingFacade(
            ILogger<DataMappingFacade> logger,
            DataMappingService dataMappingService,
            ReservedWordService reservedWordService)
        {
            _logger = logger;
            _dataMappingService = dataMappingService;
            _reservedWordService = reservedWordService;
        }

        public DataMappingScenario? SelectedMappingScenario
        {
            get => _selectedMappingScenario;
            set { _selectedMappingScenario = value; OnPropertyChanged(); }
        }

        public string MappingScenariosCount => $"{MappingScenarios.Count(s => s.IsEnabled)}";

        public event EventHandler<bool>? DataMappingWindowCloseRequested;

        public void InitializeScenarios()
        {
            try
            {
                MappingScenarios.Clear();
                foreach (var scenario in _dataMappingService.Scenarios)
                {
                    MappingScenarios.Add(scenario);
                }
                OnPropertyChanged(nameof(MappingScenariosCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing mapping scenarios");
            }
        }

        public void AddScenario()
        {
            try
            {
                if (MappingScenarios.Count >= 10)
                {
                    _logger.LogWarning("Maximum 10 scenarios reached");
                    return;
                }

                var newScenario = new DataMappingScenario
                {
                    IsEnabled = true,
                    Name = $"Scenario {MappingScenarios.Count + 1}",
                    Source = DataSource.Serial,
                    Identifier = "",
                    ValueTemplate = "",
                    TransmissionType = TransmissionType.Api,
                    ApiMethod = "GET",
                    ApiUrl = "",
                    ApiEndpoint = ""
                };

                MappingScenarios.Add(newScenario);
                SelectedMappingScenario = newScenario;
                OnPropertyChanged(nameof(MappingScenariosCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding mapping scenario");
            }
        }

        public void DeleteScenario()
        {
            try
            {
                if (SelectedMappingScenario != null)
                {
                    MappingScenarios.Remove(SelectedMappingScenario);
                    SelectedMappingScenario = null;
                    OnPropertyChanged(nameof(MappingScenariosCount));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mapping scenario");
            }
        }

        public void MoveUpScenario()
        {
            try
            {
                if (SelectedMappingScenario == null) return;
                var idx = MappingScenarios.IndexOf(SelectedMappingScenario);
                if (idx <= 0) return;
                MappingScenarios.Move(idx, idx - 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving scenario up");
            }
        }

        public void MoveDownScenario()
        {
            try
            {
                if (SelectedMappingScenario == null) return;
                var idx = MappingScenarios.IndexOf(SelectedMappingScenario);
                if (idx < 0 || idx >= MappingScenarios.Count - 1) return;
                MappingScenarios.Move(idx, idx + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving scenario down");
            }
        }

        public void CopyScenario()
        {
            try
            {
                if (SelectedMappingScenario == null) return;

                if (MappingScenarios.Count >= 10)
                {
                    _logger.LogWarning("Maximum 10 scenarios reached");
                    return;
                }

                var source = SelectedMappingScenario;
                var copy = new DataMappingScenario
                {
                    IsEnabled = source.IsEnabled,
                    Name = $"{source.Name} (Copy)",
                    Source = source.Source,
                    Identifier = source.Identifier,
                    ValueTemplate = source.ValueTemplate,
                    TransmissionType = source.TransmissionType,
                    ApiMethod = source.ApiMethod,
                    ApiUrl = source.ApiUrl,
                    ApiEndpoint = source.ApiEndpoint,
                    ApiHeaders = source.ApiHeaders,
                    ContentType = source.ContentType,
                    AuthToken = source.AuthToken,
                    TimeoutSeconds = source.TimeoutSeconds,
                    RetryCount = source.RetryCount
                };

                var idx = MappingScenarios.IndexOf(source);
                MappingScenarios.Insert(idx + 1, copy);
                SelectedMappingScenario = copy;
                OnPropertyChanged(nameof(MappingScenariosCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying scenario");
            }
        }

        public async Task<List<TestResultDisplay>> TestMappingAsync(string testData)
        {
            try
            {
                var results = await _dataMappingService.ProcessDataAsync(testData, DataSource.Serial);
                _logger.LogInformation("Test mapping: {Count} matches found", results.Count);

                return results.Select(r => BuildTestDisplay(r)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing mapping scenarios");
                return new List<TestResultDisplay>
                {
                    new TestResultDisplay
                    {
                        ScenarioName = "Error",
                        IsApi = false,
                        ResolvedData = $"// Error: {ex.Message}"
                    }
                };
            }
        }

        private TestResultDisplay BuildTestDisplay(Services.MappingResult result)
        {
            if (!result.Success)
            {
                return new TestResultDisplay
                {
                    ScenarioName = result.ScenarioName,
                    ResolvedData = $"// Error: {result.ErrorMessage}"
                };
            }

            if (result.TransmissionType == TransmissionType.Api)
            {
                // URL의 {data}는 원본 데이터(originalData)로 치환 (실제 전송 로직과 일치)
                var url = result.ApiEndpoint ?? "/";
                url = _reservedWordService.ProcessReservedWords(url);
                url = url.Replace("{data}", result.OriginalData);

                // body 내용이 있을 때만 -d 출력
                var curlData = string.IsNullOrEmpty(result.ProcessedData)
                    ? $"curl -X {result.ApiMethod} \"{url}\""
                    : $"curl -X {result.ApiMethod} \"{url}\" -d \"{result.ProcessedData}\"";

                return new TestResultDisplay
                {
                    ScenarioName = result.ScenarioName,
                    IsApi = true,
                    ResolvedData = curlData
                };
            }
            else
            {
                return new TestResultDisplay
                {
                    ScenarioName = result.ScenarioName,
                    IsApi = false,
                    ResolvedData = $"TX -> \"{result.ProcessedData}\""
                };
            }
        }

        public void SaveMapping()
        {
            try
            {
                _dataMappingService.ClearScenarios();
                foreach (var scenario in MappingScenarios)
                {
                    _dataMappingService.AddScenario(scenario);
                }
                _dataMappingService.SaveScenariosToFile();
                _logger.LogInformation("Saved {Count} mapping scenarios to file", MappingScenarios.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving mapping scenarios");
            }
        }

        public void Apply()
        {
            try
            {
                SaveMapping();
                DataMappingWindowCloseRequested?.Invoke(this, true);
                _logger.LogInformation("Data mapping changes applied and saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying data mapping changes");
            }
        }

        public void Cancel()
        {
            try
            {
                DataMappingWindowCloseRequested?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing data mapping window");
            }
        }

        public string GetApiEndpointForScenario(DataMappingScenario scenario, string data)
        {
            var baseUrl = scenario.ApiUrl?.TrimEnd('/') ?? "";
            var path = scenario.ApiEndpoint?.Trim() ?? "";

            // ApiEndpoint가 이미 전체 URL(http:// 또는 https:// 포함)인 경우 그대로 사용
            if (path.Contains("://"))
                return path;

            var trimmedPath = path.TrimStart('/');

            if (string.IsNullOrEmpty(baseUrl))
            {
                // ApiUrl이 없고 ApiEndpoint가 호스트명처럼 보이는 경우(예: "host:port/path")
                // 전체 URL로 간주하고 http://를 자동으로 추가
                if (!string.IsNullOrEmpty(trimmedPath) && LooksLikeHostUrl(trimmedPath))
                    return $"http://{trimmedPath}";

                return string.IsNullOrEmpty(trimmedPath) ? "" : "/" + trimmedPath;
            }

            return string.IsNullOrEmpty(trimmedPath) ? baseUrl : $"{baseUrl}/{trimmedPath}";
        }

        /// <summary>
        /// 주어진 경로가 호스트 URL 형태인지 확인 (예: "host.com/path", "host:port/path")
        /// </summary>
        private static bool LooksLikeHostUrl(string path)
        {
            // 호스트 부분 추출 (첫 번째 '/' 이전까지)
            var firstSlash = path.IndexOf('/');
            var hostPart = firstSlash >= 0 ? path.Substring(0, firstSlash) : path;

            // 호스트명 패턴: 점(.)을 포함하거나 "host:port" 형식
            return hostPart.Contains('.') ||
                   (hostPart.Contains(':') && hostPart.IndexOf(':') < hostPart.Length - 1);
        }

        /// <summary>내부 DataMappingService 접근자</summary>
        public DataMappingService GetService() => _dataMappingService;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
