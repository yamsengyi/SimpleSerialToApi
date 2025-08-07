namespace SimpleSerialToApi.Services.Exceptions
{
    /// <summary>
    /// Types of serial communication errors
    /// </summary>
    public enum SerialErrorType
    {
        PortNotFound,
        PortAccessDenied,
        PortAlreadyOpen,
        PortClosed,
        ReadTimeout,
        WriteTimeout,
        InvalidData,
        CommunicationLost,
        ConfigurationError,
        DeviceNotResponding,
        ChecksumError,
        ProtocolError
    }

    /// <summary>
    /// Exception thrown when serial communication operations fail
    /// </summary>
    public class SerialCommunicationException : Exception
    {
        public string PortName { get; }
        public SerialErrorType ErrorType { get; }
        public string? AdditionalData { get; }

        public SerialCommunicationException(string portName, SerialErrorType errorType, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            ErrorType = errorType;
        }

        public SerialCommunicationException(string portName, SerialErrorType errorType, string message, string? additionalData, Exception? innerException = null)
            : base(message, innerException)
        {
            PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            ErrorType = errorType;
            AdditionalData = additionalData;
        }

        public override string ToString()
        {
            var result = $"SerialCommunicationException: {ErrorType} on port {PortName}: {Message}";
            
            if (!string.IsNullOrEmpty(AdditionalData))
            {
                result += $" (Additional Data: {AdditionalData})";
            }

            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }

            return result;
        }
    }

    /// <summary>
    /// Exception thrown when API communication operations fail
    /// </summary>
    public class ApiCommunicationException : Exception
    {
        public string EndpointName { get; }
        public string EndpointUrl { get; }
        public string HttpMethod { get; }
        public int? StatusCode { get; }
        public string? ResponseContent { get; }
        public TimeSpan? ResponseTime { get; }

        public ApiCommunicationException(string endpointName, string endpointUrl, string httpMethod, string message)
            : base(message)
        {
            EndpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            EndpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        }

        public ApiCommunicationException(string endpointName, string endpointUrl, string httpMethod, int? statusCode, string message, string? responseContent = null, TimeSpan? responseTime = null)
            : base(message)
        {
            EndpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            EndpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
            StatusCode = statusCode;
            ResponseContent = responseContent;
            ResponseTime = responseTime;
        }

        public ApiCommunicationException(string endpointName, string endpointUrl, string httpMethod, string message, Exception innerException)
            : base(message, innerException)
        {
            EndpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            EndpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
            HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        }

        public override string ToString()
        {
            var result = $"ApiCommunicationException: {HttpMethod} {EndpointName} ({EndpointUrl}): {Message}";

            if (StatusCode.HasValue)
            {
                result += $" (Status: {StatusCode})";
            }

            if (ResponseTime.HasValue)
            {
                result += $" (Response Time: {ResponseTime.Value.TotalMilliseconds}ms)";
            }

            if (!string.IsNullOrEmpty(ResponseContent))
            {
                result += $"\nResponse Content: {ResponseContent}";
            }

            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }

            return result;
        }
    }

    /// <summary>
    /// Exception thrown when configuration operations fail
    /// </summary>
    public class ConfigurationException : Exception
    {
        public string SectionName { get; }
        public string SettingName { get; }
        public string? SettingValue { get; }
        public string? ExpectedType { get; }

        public ConfigurationException(string sectionName, string settingName, string message)
            : base(message)
        {
            SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            SettingName = settingName ?? throw new ArgumentNullException(nameof(settingName));
        }

        public ConfigurationException(string sectionName, string settingName, string message, Exception innerException)
            : base(message, innerException)
        {
            SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            SettingName = settingName ?? throw new ArgumentNullException(nameof(settingName));
        }

        public ConfigurationException(string sectionName, string settingName, string settingValue, string expectedType, string message)
            : base(message)
        {
            SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            SettingName = settingName ?? throw new ArgumentNullException(nameof(settingName));
            SettingValue = settingValue;
            ExpectedType = expectedType;
        }

        public override string ToString()
        {
            var result = $"ConfigurationException: {SectionName}.{SettingName}: {Message}";

            if (!string.IsNullOrEmpty(SettingValue))
            {
                result += $" (Value: '{SettingValue}')";
            }

            if (!string.IsNullOrEmpty(ExpectedType))
            {
                result += $" (Expected Type: {ExpectedType})";
            }

            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }

            return result;
        }
    }

    /// <summary>
    /// Exception thrown when queue operations fail
    /// </summary>
    public class QueueOperationException : Exception
    {
        public string QueueName { get; }
        public string Operation { get; }
        public int? MessageCount { get; }
        public int? QueueCapacity { get; }

        public QueueOperationException(string queueName, string operation, string message)
            : base(message)
        {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public QueueOperationException(string queueName, string operation, string message, Exception innerException)
            : base(message, innerException)
        {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public QueueOperationException(string queueName, string operation, int? messageCount, int? queueCapacity, string message)
            : base(message)
        {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            MessageCount = messageCount;
            QueueCapacity = queueCapacity;
        }

        public override string ToString()
        {
            var result = $"QueueOperationException: {Operation} on queue '{QueueName}': {Message}";

            if (MessageCount.HasValue)
            {
                result += $" (Message Count: {MessageCount})";
            }

            if (QueueCapacity.HasValue)
            {
                result += $" (Queue Capacity: {QueueCapacity})";
            }

            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }

            return result;
        }
    }

    /// <summary>
    /// Exception thrown when data parsing operations fail
    /// </summary>
    public class DataParsingException : Exception
    {
        public string DataFormat { get; }
        public string? RawData { get; }
        public string? ParserName { get; }
        public int? DataPosition { get; }

        public DataParsingException(string dataFormat, string message)
            : base(message)
        {
            DataFormat = dataFormat ?? throw new ArgumentNullException(nameof(dataFormat));
        }

        public DataParsingException(string dataFormat, string message, Exception innerException)
            : base(message, innerException)
        {
            DataFormat = dataFormat ?? throw new ArgumentNullException(nameof(dataFormat));
        }

        public DataParsingException(string dataFormat, string? rawData, string? parserName, string message)
            : base(message)
        {
            DataFormat = dataFormat ?? throw new ArgumentNullException(nameof(dataFormat));
            RawData = rawData;
            ParserName = parserName;
        }

        public DataParsingException(string dataFormat, string? rawData, string? parserName, int? dataPosition, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            DataFormat = dataFormat ?? throw new ArgumentNullException(nameof(dataFormat));
            RawData = rawData;
            ParserName = parserName;
            DataPosition = dataPosition;
        }

        public override string ToString()
        {
            var result = $"DataParsingException: Failed to parse {DataFormat} data: {Message}";

            if (!string.IsNullOrEmpty(ParserName))
            {
                result += $" (Parser: {ParserName})";
            }

            if (DataPosition.HasValue)
            {
                result += $" (Position: {DataPosition})";
            }

            if (!string.IsNullOrEmpty(RawData))
            {
                result += $"\nRaw Data: {RawData}";
            }

            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }

            return result;
        }
    }
}