using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 데이터 매핑 시나리오 관리를 위한 파사드 인터페이스
    /// </summary>
    public interface IDataMappingFacade
    {
        /// <summary>새 시나리오 추가</summary>
        void AddScenario();

        /// <summary>선택 시나리오 삭제</summary>
        void DeleteScenario();

        /// <summary>선택 시나리오 위로 이동</summary>
        void MoveUpScenario();

        /// <summary>선택 시나리오 아래로 이동</summary>
        void MoveDownScenario();

        /// <summary>선택 시나리오 복사</summary>
        void CopyScenario();

        /// <summary>매핑 테스트 실행 (테스트 데이터 입력, 결과 반환)</summary>
        Task<List<TestResultDisplay>> TestMappingAsync(string testData);

        /// <summary>시나리오 파일 저장</summary>
        void SaveMapping();

        /// <summary>적용 후 창 닫기</summary>
        void Apply();

        /// <summary>취소 후 창 닫기</summary>
        void Cancel();

        /// <summary>서비스에서 시나리오 로드</summary>
        void InitializeScenarios();

        /// <summary>시나리오별 API 엔드포인트 조합</summary>
        string GetApiEndpointForScenario(DataMappingScenario scenario, string data);

        /// <summary>매핑 시나리오 컬렉션</summary>
        ObservableCollection<DataMappingScenario> MappingScenarios { get; }

        /// <summary>선택된 시나리오</summary>
        DataMappingScenario? SelectedMappingScenario { get; set; }

        /// <summary>데이터 소스 목록</summary>
        List<DataSource> DataSources { get; }

        /// <summary>전송 타입 목록</summary>
        List<TransmissionType> TransmissionTypes { get; }

        /// <summary>API 메서드 목록</summary>
        List<string> ApiMethods { get; }

        /// <summary>Content-Type 목록</summary>
        List<string> ContentTypes { get; }

        /// <summary>활성화된 시나리오 수</summary>
        string MappingScenariosCount { get; }

        /// <summary>데이터 매핑 창 닫기 요청 이벤트</summary>
        event EventHandler<bool>? DataMappingWindowCloseRequested;
    }
}
