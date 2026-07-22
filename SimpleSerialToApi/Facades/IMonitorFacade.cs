namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 시리얼/API 모니터 관리를 위한 파사드 인터페이스
    /// </summary>
    public interface IMonitorFacade
    {
        // Serial Monitor
        void SaveSerialMonitor();
        void SaveApiMonitor();
        void ClearSerialMonitor();
        void ClearApiMonitor();
        void LoadExistingSerialMessages();
        void LoadExistingApiMessages();

        // Serial Monitor Properties
        string SerialMonitorText { get; set; }
        string ApiMonitorText { get; set; }
        bool SerialMonitorAutoScroll { get; set; }
        bool ApiMonitorAutoScroll { get; set; }
        string SerialMonitorStatus { get; set; }
        string ApiMonitorStatus { get; set; }
        string SerialMonitorFilter { get; set; }
        string ApiMonitorFilter { get; set; }
        System.Collections.Generic.List<string> SerialMonitorFilters { get; }
        System.Collections.Generic.List<string> ApiMonitorFilters { get; }
        bool SerialShowTimestamps { get; set; }
        bool ApiShowHeaders { get; set; }
        string SerialMessageCount { get; }
        string ApiRequestCount { get; }
        string ApiSuccessRate { get; }

        /// <summary>시리얼 모니터 메시지 추가 처리 (이벤트 핸들러용)</summary>
        void OnSerialMonitorMessageAdded(Models.MonitorMessage message);

        /// <summary>API 모니터 메시지 추가 처리 (이벤트 핸들러용)</summary>
        void OnApiMonitorMessageAdded(Services.ApiMonitorMessage message);
    }
}
