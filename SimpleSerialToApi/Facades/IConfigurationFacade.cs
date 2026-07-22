using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 앱 설정 관리를 위한 파사드 인터페이스
    /// </summary>
    public interface IConfigurationFacade
    {
        /// <summary>API URL 로드</summary>
        void LoadApiUrl();

        /// <summary>Queue 설정 로드</summary>
        void LoadQueueSettings();

        /// <summary>전송 간격 설정 저장</summary>
        void SetTransmissionInterval();

        /// <summary>배치 크기 설정 저장</summary>
        void SetBatchSize();

        /// <summary>Device ID 설정 저장</summary>
        void SetDeviceId();

        /// <summary>시리얼 설정 창 열기</summary>
        void OpenSerialConfig();

        /// <summary>API URL (연결 테스트 전용)</summary>
        string ApiUrl { get; set; }

        /// <summary>전송 간격</summary>
        string TransmissionInterval { get; set; }

        /// <summary>배치 크기</summary>
        string BatchSize { get; set; }

        /// <summary>Device ID</summary>
        string DeviceId { get; set; }
    }
}
