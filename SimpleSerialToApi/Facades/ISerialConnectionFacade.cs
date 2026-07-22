using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 시리얼 연결/포트 관리를 위한 파사드 인터페이스
    /// </summary>
    public interface ISerialConnectionFacade
    {
        /// <summary>시리얼 포트 연결</summary>
        Task ConnectAsync();

        /// <summary>시리얼 포트 해제</summary>
        Task DisconnectAsync();

        /// <summary>포트 목록 갱신</summary>
        void RefreshPorts();

        /// <summary>최적 포트 자동 선택</summary>
        void PerformSmartSelection();

        /// <summary>초기 스마트 선택</summary>
        void InitializeSmartPortSelection();

        /// <summary>자동 연결 확인</summary>
        Task CheckAutoConnectAsync();

        /// <summary>연결 가능 여부</summary>
        bool CanConnect { get; }

        /// <summary>연결 해제 가능 여부</summary>
        bool CanDisconnect { get; }

        /// <summary>선택된 시리얼 포트</summary>
        string SerialPort { get; set; }

        /// <summary>연결 상태</summary>
        bool IsConnected { get; set; }

        /// <summary>사용 가능한 포트 목록</summary>
        ObservableCollection<ComPortInfo> AvailablePorts { get; }

        /// <summary>시리얼 연결 상태 문자열</summary>
        string SerialConnectionStatus { get; }
    }
}
