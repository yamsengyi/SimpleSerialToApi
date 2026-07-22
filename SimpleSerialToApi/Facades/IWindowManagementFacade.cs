namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 팝업 창 관리를 위한 파사드 인터페이스
    /// </summary>
    public interface IWindowManagementFacade
    {
        /// <summary>데이터 매핑 창 열기 또는 포커스</summary>
        void OpenDataMapping(object dataContext);

        /// <summary>예약어 창 열기 또는 포커스</summary>
        void ShowReservedWords();

        /// <summary>시리얼 모니터 창 열기 또는 포커스</summary>
        void OpenSerialMonitor(object dataContext);

        /// <summary>API 모니터 창 열기 또는 포커스</summary>
        void OpenApiMonitor(object dataContext);

        /// <summary>모든 열린 창 닫기</summary>
        void CloseAllChildWindows();
    }
}
