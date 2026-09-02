using System.Threading.Tasks;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 데이터 전송 처리를 위한 파사드 인터페이스
    /// </summary>
    public interface IDataTransmissionFacade
    {
        /// <summary>시리얼 데이터 수신 이벤트 처리</summary>
        Task OnSerialDataReceivedAsync(byte[] data);

        /// <summary>매핑 완료 이벤트 처리</summary>
        Task OnMappingProcessedAsync(MappingProcessedEventArgs e);

        /// <summary>시뮬레이션 데이터 수신 처리</summary>
        Task OnSimulatedDataReceivedAsync(SimulatedSerialDataEventArgs e);

        /// <summary>큐 매니저 초기화 및 처리 시작</summary>
        Task InitializeQueueProcessingAsync();

        /// <summary>큐 카운트 갱신</summary>
        void UpdateQueueCount();

        /// <summary>API 연결 테스트</summary>
        Task TestApiAsync(string testApiUrl);

        /// <summary>메시지 큐 클리어</summary>
        Task ClearQueueAsync();

        /// <summary>모든 모니터 로그 클리어</summary>
        void ClearLogs();

        /// <summary>메시지 큐와 모든 모니터 로그 클리어</summary>
        Task ClearLogsAsync();

        /// <summary>현재 큐 카운트</summary>
        int QueueCount { get; }

        /// <summary>큐 카운트 변경 이벤트</summary>
        event System.EventHandler<int>? QueueCountChanged;

        /// <summary>상태 변경 이벤트</summary>
        event System.EventHandler<string>? StatusChanged;
    }
}
