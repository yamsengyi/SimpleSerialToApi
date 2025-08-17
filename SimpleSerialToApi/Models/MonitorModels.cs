using System;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// 메시지 방향
    /// </summary>
    public enum MessageDirection
    {
        Send,
        Receive
    }

    /// <summary>
    /// 메시지 타입
    /// </summary>
    public enum MessageType
    {
        Serial,
        Api,
        Mapped,
        System
    }

    /// <summary>
    /// 모니터 메시지 모델
    /// </summary>
    public class MonitorMessage
    {
        public DateTime Timestamp { get; set; }
        public MessageDirection Direction { get; set; }
        public MessageType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;

        public string FormattedMessage => 
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{GetDirectionTag()}] {Content}" +
            (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");

        private string GetDirectionTag()
        {
            return Type switch
            {
                MessageType.Serial => Direction == MessageDirection.Send ? "TX" : "RX",
                MessageType.Api => Direction == MessageDirection.Send ? "REQ" : "RES",
                MessageType.Mapped => "MAPPED",
                MessageType.System => "SYS",
                _ => "UNK"
            };
        }
    }

    /// <summary>
    /// 통신 이벤트 모델
    /// </summary>
    public class CommunicationEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string ProcessedData { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
