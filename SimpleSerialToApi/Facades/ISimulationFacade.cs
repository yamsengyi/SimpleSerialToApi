namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 시뮬레이션 관리를 위한 파사드 인터페이스
    /// </summary>
    public interface ISimulationFacade
    {
        /// <summary>시뮬레이션 시작</summary>
        void Start();

        /// <summary>시뮬레이션 중지</summary>
        void Stop();

        /// <summary>시뮬레이션 토글 (시작/중지)</summary>
        void Toggle();

        /// <summary>단일 시뮬레이션 데이터 생성</summary>
        void GenerateSingleData();

        /// <summary>시뮬레이션 실행 중 여부</summary>
        bool IsSimulating { get; }

        /// <summary>시뮬레이션 간격 (초)</summary>
        string SimulationInterval { get; set; }

        /// <summary>시뮬레이션 버튼 텍스트</summary>
        string SimulationButtonText { get; }
    }
}
