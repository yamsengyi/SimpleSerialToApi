using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 주기적으로 큐의 데이터를 HTTP API로 전송하는 타이머 서비스
    /// </summary>
    public class TimerService : IDisposable
    {
        private readonly ILogger<TimerService> _logger;
        private readonly SimpleHttpService _httpService;
        private readonly SimpleQueueService _queueService;
        private Timer? _timer;
        private bool _disposed = false;

        public TimerService(
            ILogger<TimerService> logger,
            SimpleHttpService httpService,
            SimpleQueueService queueService)
        {
            _logger = logger;
            _httpService = httpService;
            _queueService = queueService;
        }

        /// <summary>
        /// 타이머 시작 (기본 5초 간격)
        /// </summary>
        public void Start(int intervalSeconds = 5)
        {
            if (_timer != null)
                return;

            _timer = new Timer(ProcessQueue, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
            _logger.LogInformation("Timer started with {Interval} seconds interval", intervalSeconds);
        }

        /// <summary>
        /// 타이머 중지
        /// </summary>
        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            _logger.LogInformation("Timer stopped");
        }

        /// <summary>
        /// 큐 처리 (타이머 콜백)
        /// </summary>
        private async void ProcessQueue(object? state)
        {
            try
            {
                // 큐에서 모든 데이터 가져오기
                var messages = _queueService.DequeueAll();

                // HTTP로 일괄 전송
                if (messages.Count > 0)
                {
                    await SendDataAsync(messages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue");
            }
        }

        /// <summary>
        /// HTTP API로 데이터 전송
        /// </summary>
        private async Task SendDataAsync(List<string> messages)
        {
            try
            {
                var jsonData = System.Text.Json.JsonSerializer.Serialize(new { 
                    data = messages, 
                    timestamp = DateTime.Now,
                    count = messages.Count 
                });
                
                var success = await _httpService.SendJsonAsync(jsonData);
                
                if (success)
                {
                    _logger.LogInformation("Successfully sent {Count} messages to API", messages.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to send {Count} messages to API", messages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data to API");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
