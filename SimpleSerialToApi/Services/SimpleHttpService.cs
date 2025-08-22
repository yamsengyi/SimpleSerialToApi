using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 간단한 HTTP API 클라이언트 서비스
    /// </summary>
    public class SimpleHttpService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SimpleHttpService> _logger;
        private readonly ApiMonitorService _apiMonitorService;
        private readonly ApiFileLogService _apiFileLogService;
        private string _apiUrl = "http://localhost:8080/api/data"; // 기본값

        public SimpleHttpService(ILogger<SimpleHttpService> logger, ApiMonitorService apiMonitorService, ApiFileLogService apiFileLogService)
        {
            // HttpClient 설정: 자동 리다이렉션 허용 (명시적 설정)
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10  // 무한 루프 방지
            };
            
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);  // 타임아웃 설정
            _logger = logger;
            _apiMonitorService = apiMonitorService;
            _apiFileLogService = apiFileLogService;
        }

        /// <summary>
        /// API URL 설정
        /// </summary>
        public void SetApiUrl(string url)
        {
            _apiUrl = url;
        }

        /// <summary>
        /// JSON 데이터를 API로 전송
        /// </summary>
        public async Task<bool> SendJsonAsync(string jsonData)
        {
            return await SendDataAsync(jsonData, "application/json");
        }

        /// <summary>
        /// 지정된 Content-Type으로 데이터를 API로 전송
        /// </summary>
        public async Task<bool> SendDataAsync(string data, string contentType = "application/json")
        {
            var requestId = Guid.NewGuid().ToString("N")[..8]; // 8자리 요청 ID
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // 파일 로그: 요청
                await _apiFileLogService.LogRequestAsync(requestId, "POST", _apiUrl, data, contentType);
                
                var content = new StringContent(data ?? string.Empty, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(_apiUrl, content);
                
                stopwatch.Stop();

                // Response 내용 읽기
                string responseBody = string.Empty;
                try
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to read response body: {Error}", ex.Message);
                    responseBody = $"[Error reading response: {ex.Message}]";
                }

                // 파일 로그: 응답
                await _apiFileLogService.LogResponseAsync(requestId, response.StatusCode, responseBody, stopwatch.Elapsed);

                // API 모니터에 Response 로깅
                _apiMonitorService.LogApiResponse(requestId, response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send data to API. Status: {Status}, Reason: {Reason}, RequestId: {RequestId}, ContentType: {ContentType}, FullUrl: {FullUrl}", 
                        response.StatusCode, response.ReasonPhrase, requestId, contentType, _apiUrl);
                    return false;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error sending data to API, RequestId: {RequestId}, ContentType: {ContentType}, FullUrl: {FullUrl}", requestId, contentType, _apiUrl);
                
                // 파일 로그: 에러
                await _apiFileLogService.LogErrorAsync(requestId, ex);
                
                // 에러도 API 모니터에 로깅
                _apiMonitorService.LogApiError(requestId, ex);
                return false;
            }
        }

        /// <summary>
        /// 연결 테스트 - HTTP 응답이 있으면 연결 성공으로 판단
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                
                // 리다이렉션 정보도 포함하여 로깅
                var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? _apiUrl;
                var isRedirected = !string.Equals(_apiUrl, finalUrl, StringComparison.OrdinalIgnoreCase);
                
                // HTTP 응답을 받았다면 연결은 성공 (상태 코드에 관계없이)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("API connection test failed for {Url}: {Error}", _apiUrl, ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
