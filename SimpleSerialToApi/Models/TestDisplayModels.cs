using System.Windows.Media;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// 매핑 테스트 결과 표시용 모델 (resolved values for display)
    /// </summary>
    public class TestResultDisplay
    {
        public string ScenarioName { get; set; } = string.Empty;
        public bool IsApi { get; set; }
        public string ResolvedData { get; set; } = string.Empty;
    }
}
