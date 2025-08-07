namespace SimpleSerialToApi.ViewModels
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public enum ApplicationState
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }

    public class PerformanceMetrics
    {
        public int MessagesReceived { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesInQueue { get; set; }
        public int SuccessfulTransmissions { get; set; }
        public int FailedTransmissions { get; set; }
        public double SuccessRate => MessagesSent > 0 ? (double)SuccessfulTransmissions / MessagesSent * 100 : 0;
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class ApiEndpointStatus
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public ConnectionStatus Status { get; set; }
        public DateTime? LastSuccessfulCall { get; set; }
        public string? LastError { get; set; }
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public double AverageResponseTime { get; set; }
    }
}