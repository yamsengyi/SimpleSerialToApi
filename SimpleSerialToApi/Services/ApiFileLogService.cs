using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// API 통신 송/수신에 대한 파일 로그 서비스
    /// </summary>
    public class ApiFileLogService
    {
        private readonly ILogger<ApiFileLogService> _logger;
        private readonly string _logDirectory;
        private readonly object _fileLock = new object();

        public ApiFileLogService(ILogger<ApiFileLogService> logger)
        {
            _logger = logger;
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "API");
            
            // 로그 디렉토리 생성
            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// API 요청을 파일로 로그
        /// </summary>
        public async Task LogRequestAsync(string requestId, string method, string fullUrl, string? requestBody, string contentType)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = $"api_requests_{DateTime.Now:yyyyMMdd}.log";
                var filePath = Path.Combine(_logDirectory, fileName);

                var logEntry = $"[{timestamp}] REQUEST {requestId}\n" +
                              $"Method: {method}\n" +
                              $"URL: {fullUrl}\n" +
                              $"Content-Type: {contentType}\n" +
                              $"Body: {requestBody ?? "[empty]"}\n" +
                              $"----------------------------------------\n\n";

                await WriteToFileAsync(filePath, logEntry);
                
                _logger.LogDebug("API request logged to file: {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log API request to file: {RequestId}", requestId);
            }
        }

        /// <summary>
        /// API 응답을 파일로 로그
        /// </summary>
        public async Task LogResponseAsync(string requestId, HttpStatusCode statusCode, string? responseBody, TimeSpan responseTime)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = $"api_responses_{DateTime.Now:yyyyMMdd}.log";
                var filePath = Path.Combine(_logDirectory, fileName);

                var logEntry = $"[{timestamp}] RESPONSE {requestId}\n" +
                              $"Status: {(int)statusCode} {statusCode}\n" +
                              $"Response Time: {responseTime.TotalMilliseconds:F0}ms\n" +
                              $"Body: {responseBody ?? "[empty]"}\n" +
                              $"----------------------------------------\n\n";

                await WriteToFileAsync(filePath, logEntry);
                
                _logger.LogDebug("API response logged to file: {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log API response to file: {RequestId}", requestId);
            }
        }

        /// <summary>
        /// API 에러를 파일로 로그
        /// </summary>
        public async Task LogErrorAsync(string requestId, Exception exception)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = $"api_errors_{DateTime.Now:yyyyMMdd}.log";
                var filePath = Path.Combine(_logDirectory, fileName);

                var logEntry = $"[{timestamp}] ERROR {requestId}\n" +
                              $"Exception: {exception.GetType().Name}\n" +
                              $"Message: {exception.Message}\n" +
                              $"StackTrace: {exception.StackTrace}\n" +
                              $"----------------------------------------\n\n";

                await WriteToFileAsync(filePath, logEntry);
                
                _logger.LogDebug("API error logged to file: {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log API error to file: {RequestId}", requestId);
            }
        }

        /// <summary>
        /// 파일에 안전하게 쓰기 (동시성 제어)
        /// </summary>
        private async Task WriteToFileAsync(string filePath, string content)
        {
            await Task.Run(() =>
            {
                lock (_fileLock)
                {
                    File.AppendAllText(filePath, content);
                }
            });
        }

        /// <summary>
        /// 오래된 로그 파일 정리 (30일 이상)
        /// </summary>
        public async Task CleanupOldLogsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(_logDirectory, "*.log");
                    var cutoffDate = DateTime.Now.AddDays(-30);

                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(file);
                            _logger.LogInformation("Deleted old API log file: {FileName}", fileInfo.Name);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old API log files");
            }
        }
    }
}
