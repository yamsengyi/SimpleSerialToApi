namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// Configuration for API endpoints
    /// </summary>
    public class ApiEndpointConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "POST";
        public string AuthType { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public int Timeout { get; set; } = 30000;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Configuration for data mapping rules
    /// </summary>
    public class MappingRuleConfig
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";
        public string Converter { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = false;
    }

    /// <summary>
    /// Configuration for message queue settings
    /// </summary>
    public class MessageQueueConfig
    {
        public int MaxQueueSize { get; set; } = 1000;
        public int BatchSize { get; set; } = 10;
        public int RetryCount { get; set; } = 3;
        public int RetryInterval { get; set; } = 5000;
    }

    /// <summary>
    /// Overall application configuration
    /// </summary>
    public class ApplicationConfig
    {
        public SerialConnectionSettings SerialSettings { get; set; } = new SerialConnectionSettings();
        public List<ApiEndpointConfig> ApiEndpoints { get; set; } = new List<ApiEndpointConfig>();
        public List<MappingRuleConfig> MappingRules { get; set; } = new List<MappingRuleConfig>();
        public MessageQueueConfig MessageQueueSettings { get; set; } = new MessageQueueConfig();
        public string LogLevel { get; set; } = "Information";
    }
}