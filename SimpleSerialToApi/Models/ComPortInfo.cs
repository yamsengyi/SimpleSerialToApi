namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// COM 포트 정보를 담는 모델
    /// </summary>
    public class ComPortInfo
    {
        public string PortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSmartSelected { get; set; } = false;
        public bool IsLastUsed { get; set; } = false;

        public string DisplayName 
        { 
            get 
            {
                var displayText = Description.Contains(PortName) ? Description : $"{PortName} - {Description}";
                
                if (IsLastUsed && IsSmartSelected)
                    return $"⭐ {displayText} (마지막 사용)";
                else if (IsSmartSelected)
                    return $"🔧 {displayText} (스마트 선택)";
                else
                    return displayText;
            }
        }
        
        public override string ToString() => DisplayName;
    }
}
