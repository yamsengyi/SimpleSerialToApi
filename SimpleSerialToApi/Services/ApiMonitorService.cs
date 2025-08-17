using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// API 통신 모니터링 서비스
    /// </summary>
    public class ApiMonitorService
    {
        private readonly ILogger<ApiMonitorService> _logger;
        private readonly ObservableCollection<ApiMonitorMessage> _messages;
        private readonly int _maxMessages;
        private bool _isEnabled = true;

        /// <summary>
        /// 새 API 메시지 추가 이벤트
        /// </summary>
        public event EventHandler<ApiMonitorMessage>? MessageAdded;

        public ApiMonitorService(ILogger<ApiMonitorService> logger)
        {
            _logger = logger;
            _messages = new ObservableCollection<ApiMonitorMessage>();
            _maxMessages = 1000;
        }

        /// <summary>
        /// API 모니터 메시지 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<ApiMonitorMessage> Messages => _messages.ToList().AsReadOnly();

        /// <summary>
        /// 모니터링 활성화 상태
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// API 요청 로깅
        /// </summary>
        /// <param name="method">HTTP 메서드</param>
        /// <param name="url">요청 URL</param>
        /// <param name="requestBody">요청 본문</param>
        /// <param name="headers">요청 헤더</param>
        /// <returns>추적할 수 있는 요청 ID</returns>
        public string LogApiRequest(string method, string url, string? requestBody = null, Dictionary<string, string>? headers = null)
        {
            if (!_isEnabled) return string.Empty;

            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            var message = new ApiMonitorMessage
            {
                RequestId = requestId,
                Timestamp = DateTime.Now,
                Method = method,
                Url = url,
                RequestBody = requestBody ?? "",
                RequestHeaders = headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                Direction = MessageDirection.Send
            };

            AddMessage(message);
            return requestId;
        }

        /// <summary>
        /// API 응답 로깅
        /// </summary>
        /// <param name="requestId">요청 ID</param>
        /// <param name="statusCode">HTTP 상태 코드</param>
        /// <param name="responseBody">응답 본문</param>
        /// <param name="headers">응답 헤더</param>
        /// <param name="responseTime">응답 시간 (밀리초)</param>
        public void LogApiResponse(string requestId, HttpStatusCode statusCode, string? responseBody = null, 
            Dictionary<string, string>? headers = null, long responseTime = 0)
        {
            if (!_isEnabled) return;

            // 기존 요청 메시지를 찾아서 응답 정보 업데이트
            var requestMessage = _messages.FirstOrDefault(m => m.RequestId == requestId);
            if (requestMessage != null)
            {
                requestMessage.StatusCode = statusCode;
                requestMessage.ResponseBody = responseBody ?? "";
                requestMessage.ResponseHeaders = headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
                requestMessage.ResponseTime = responseTime;
                requestMessage.ResponseTimestamp = DateTime.Now;
                requestMessage.IsCompleted = true;
            }
            else
            {
                // 요청 메시지를 찾을 수 없는 경우, 새로운 응답 전용 메시지 생성
                var responseMessage = new ApiMonitorMessage
                {
                    RequestId = requestId,
                    Timestamp = DateTime.Now,
                    StatusCode = statusCode,
                    ResponseBody = responseBody ?? "",
                    ResponseHeaders = headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                    ResponseTime = responseTime,
                    Direction = MessageDirection.Receive,
                    IsCompleted = true
                };

                AddMessage(responseMessage);
            }
        }

        /// <summary>
        /// API 오류 로깅
        /// </summary>
        /// <param name="requestId">요청 ID</param>
        /// <param name="error">오류 정보</param>
        public void LogApiError(string requestId, Exception error)
        {
            if (!_isEnabled) return;

            var requestMessage = _messages.FirstOrDefault(m => m.RequestId == requestId);
            if (requestMessage != null)
            {
                requestMessage.ErrorMessage = error.Message;
                requestMessage.IsCompleted = true;
                requestMessage.ResponseTimestamp = DateTime.Now;
            }
            else
            {
                var errorMessage = new ApiMonitorMessage
                {
                    RequestId = requestId,
                    Timestamp = DateTime.Now,
                    ErrorMessage = error.Message,
                    Direction = MessageDirection.Receive,
                    IsCompleted = true
                };

                AddMessage(errorMessage);
            }
        }

        /// <summary>
        /// 메시지 추가 (내부 메서드)
        /// </summary>
        /// <param name="message">추가할 메시지</param>
        private void AddMessage(ApiMonitorMessage message)
        {
            try
            {
                // 최대 메시지 수 제한
                while (_messages.Count >= _maxMessages)
                {
                    _messages.RemoveAt(0);
                }

                _messages.Add(message);
                MessageAdded?.Invoke(this, message);

                _logger.LogDebug("API monitor message added: {RequestId}", message.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding API monitor message");
            }
        }

        /// <summary>
        /// 모든 메시지 지우기
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            _logger.LogInformation("API monitor messages cleared");
        }

        /// <summary>
        /// 메시지를 파일로 저장
        /// </summary>
        /// <param name="filePath">저장할 파일 경로</param>
        /// <returns>저장 성공 여부</returns>
        public async Task<bool> SaveToFileAsync(string filePath)
        {
            try
            {
                var lines = _messages.Select(m => m.FormattedMessage).ToList();
                await File.WriteAllLinesAsync(filePath, lines);
                
                _logger.LogInformation("API monitor messages saved to: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API monitor messages to file: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 상태 코드로 메시지 필터링
        /// </summary>
        /// <param name="statusCode">필터할 상태 코드</param>
        /// <returns>필터링된 메시지 목록</returns>
        public IReadOnlyList<ApiMonitorMessage> GetMessagesByStatusCode(HttpStatusCode statusCode)
        {
            return _messages.Where(m => m.StatusCode == statusCode).ToList().AsReadOnly();
        }

        /// <summary>
        /// HTTP 메서드로 메시지 필터링
        /// </summary>
        /// <param name="method">필터할 HTTP 메서드</param>
        /// <returns>필터링된 메시지 목록</returns>
        public IReadOnlyList<ApiMonitorMessage> GetMessagesByMethod(string method)
        {
            return _messages.Where(m => m.Method.Equals(method, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
        }

        /// <summary>
        /// 시간 범위로 메시지 필터링
        /// </summary>
        /// <param name="from">시작 시간</param>
        /// <param name="to">종료 시간</param>
        /// <returns>필터링된 메시지 목록</returns>
        public IReadOnlyList<ApiMonitorMessage> GetMessagesByTimeRange(DateTime from, DateTime to)
        {
            return _messages.Where(m => m.Timestamp >= from && m.Timestamp <= to).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// API 모니터 메시지 모델
    /// </summary>
    public class ApiMonitorMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? ResponseTimestamp { get; set; }
        public MessageDirection Direction { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string RequestBody { get; set; } = string.Empty;
        public Dictionary<string, string> RequestHeaders { get; set; } = new();
        public HttpStatusCode? StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public Dictionary<string, string> ResponseHeaders { get; set; } = new();
        public long ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsCompleted { get; set; }

        public string FormattedMessage
        {
            get
            {
                if (Direction == MessageDirection.Send || !IsCompleted)
                {
                    return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [REQ] {Method} {Url}" +
                           (string.IsNullOrEmpty(RequestBody) ? "" : $"\n[BODY] {RequestBody}");
                }
                else
                {
                    var status = StatusCode?.ToString() ?? "ERROR";
                    var responseInfo = StatusCode.HasValue ? $"{(int)StatusCode.Value} {StatusCode}" : "ERROR";
                    
                    if (!string.IsNullOrEmpty(ErrorMessage))
                    {
                        responseInfo = $"ERROR: {ErrorMessage}";
                    }

                    return $"[{ResponseTimestamp:yyyy-MM-dd HH:mm:ss.fff}] [RES] {responseInfo} ({ResponseTime}ms)" +
                           (string.IsNullOrEmpty(ResponseBody) ? "" : $"\n[BODY] {ResponseBody}");
                }
            }
        }
    }
}
