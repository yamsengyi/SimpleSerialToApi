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
        private string _apiUrl = "http://localhost:8080/api/data"; // 기본값

        public SimpleHttpService(ILogger<SimpleHttpService> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        /// <summary>
        /// API URL 설정
        /// </summary>
        public void SetApiUrl(string url)
        {
            _apiUrl = url;
            _logger.LogInformation("API URL set to: {Url}", url);
        }

        /// <summary>
        /// JSON 데이터를 API로 전송
        /// </summary>
        public async Task<bool> SendJsonAsync(string jsonData)
        {
            try
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent data to API. Status: {Status}", response.StatusCode);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send data to API. Status: {Status}, Reason: {Reason}", 
                        response.StatusCode, response.ReasonPhrase);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data to API");
                return false;
            }
        }

        /// <summary>
        /// 연결 테스트
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
