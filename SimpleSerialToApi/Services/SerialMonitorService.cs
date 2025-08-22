using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 시리얼 통신 모니터링 서비스
    /// </summary>
    public class SerialMonitorService
    {
        private readonly ILogger<SerialMonitorService> _logger;
        private readonly ObservableCollection<MonitorMessage> _messages;
        private readonly int _maxMessages;
        private bool _isEnabled = true;
        private bool _autoScroll = true;

        /// <summary>
        /// 새 메시지 추가 이벤트
        /// </summary>
        public event EventHandler<MonitorMessage>? MessageAdded;

        public SerialMonitorService(ILogger<SerialMonitorService> logger)
        {
            _logger = logger;
            _messages = new ObservableCollection<MonitorMessage>();
            _maxMessages = 1000; // 최대 메시지 수 제한
        }

        /// <summary>
        /// 모니터 메시지 목록 (읽기 전용)
        /// </summary>
        public IReadOnlyList<MonitorMessage> Messages => _messages.ToList().AsReadOnly();

        /// <summary>
        /// 모니터링 활성화 상태
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// 자동 스크롤 활성화 상태
        /// </summary>
        public bool AutoScroll
        {
            get => _autoScroll;
            set => _autoScroll = value;
        }

        /// <summary>
        /// 시리얼 송신 데이터 로깅
        /// </summary>
        /// <param name="data">송신된 데이터</param>
        public void LogSerialSent(string data)
        {
            if (!_isEnabled) return;

            var message = new MonitorMessage
            {
                Timestamp = DateTime.Now,
                Direction = MessageDirection.Send,
                Type = MessageType.Serial,
                Content = data,
                AdditionalInfo = "Serial TX"
            };

            AddMessage(message);
        }

        /// <summary>
        /// 시리얼 수신 데이터 로깅
        /// </summary>
        /// <param name="data">수신된 데이터</param>
        public void LogSerialReceived(string data)
        {
            if (!_isEnabled) return;

            var message = new MonitorMessage
            {
                Timestamp = DateTime.Now,
                Direction = MessageDirection.Receive,
                Type = MessageType.Serial,
                Content = data,
                AdditionalInfo = "Serial RX"
            };

            AddMessage(message);
        }

        /// <summary>
        /// 매핑 처리 결과 로깅
        /// </summary>
        /// <param name="originalData">원본 데이터</param>
        /// <param name="processedData">처리된 데이터</param>
        /// <param name="scenarioName">시나리오 이름</param>
        public void LogMappingResult(string originalData, string processedData, string scenarioName)
        {
            if (!_isEnabled) return;

            var message = new MonitorMessage
            {
                Timestamp = DateTime.Now,
                Direction = MessageDirection.Send, // 처리 결과는 송신 방향으로 간주
                Type = MessageType.Mapped,
                Content = processedData,
                AdditionalInfo = $"Scenario: {scenarioName} | Original: {originalData}"
            };

            AddMessage(message);
        }

        /// <summary>
        /// 시스템 메시지 로깅
        /// </summary>
        /// <param name="message">시스템 메시지</param>
        public void LogSystemMessage(string message)
        {
            if (!_isEnabled) return;

            var msg = new MonitorMessage
            {
                Timestamp = DateTime.Now,
                Direction = MessageDirection.Send,
                Type = MessageType.System,
                Content = message,
                AdditionalInfo = "System"
            };

            AddMessage(msg);
        }

        /// <summary>
        /// 메시지 추가 (내부 메서드)
        /// </summary>
        /// <param name="message">추가할 메시지</param>
        private void AddMessage(MonitorMessage message)
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

                _logger.LogDebug("Serial monitor message added: {Message}", message.FormattedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding monitor message");
            }
        }

        /// <summary>
        /// 모든 메시지 지우기
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
        }

        /// <summary>
        /// 모든 로그 지우기 (Clear 메서드와 동일, 호환성을 위해 추가)
        /// </summary>
        public void ClearLogs()
        {
            Clear();
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
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving serial monitor messages to file: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 특정 조건으로 메시지 필터링
        /// </summary>
        /// <param name="filter">필터 함수</param>
        /// <returns>필터링된 메시지 목록</returns>
        public IReadOnlyList<MonitorMessage> GetFilteredMessages(Func<MonitorMessage, bool> filter)
        {
            return _messages.Where(filter).ToList().AsReadOnly();
        }

        /// <summary>
        /// 최근 N개 메시지 가져오기
        /// </summary>
        /// <param name="count">가져올 메시지 수</param>
        /// <returns>최근 메시지 목록</returns>
        public IReadOnlyList<MonitorMessage> GetRecentMessages(int count)
        {
            return _messages.TakeLast(count).ToList().AsReadOnly();
        }
    }
}
