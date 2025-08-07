using System;
using System.Collections.Generic;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// Raw data received from serial port
    /// </summary>
    public class RawSerialData
    {
        /// <summary>
        /// Time when the data was received
        /// </summary>
        public DateTime ReceivedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Raw byte data received from serial port
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Format of the data (HEX, TEXT, JSON, BINARY)
        /// </summary>
        public string DataFormat { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the device that sent the data
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Source serial port name
        /// </summary>
        public string PortName { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new instance of RawSerialData
        /// </summary>
        public RawSerialData()
        {
        }

        /// <summary>
        /// Creates a new instance of RawSerialData with the specified data
        /// </summary>
        /// <param name="data">Raw data bytes</param>
        /// <param name="dataFormat">Data format</param>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="portName">Serial port name</param>
        public RawSerialData(byte[] data, string dataFormat = "TEXT", string deviceId = "", string portName = "")
        {
            Data = data ?? Array.Empty<byte>();
            DataFormat = dataFormat;
            DeviceId = deviceId;
            PortName = portName;
            ReceivedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Data after parsing from raw serial data
    /// </summary>
    public class ParsedData
    {
        /// <summary>
        /// Timestamp of when the data was parsed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Device identifier that sent the data
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Parsed field values with their names as keys
        /// </summary>
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Source of the data (serial port, file, etc.)
        /// </summary>
        public string DataSource { get; set; } = string.Empty;

        /// <summary>
        /// Original raw data reference
        /// </summary>
        public RawSerialData? OriginalData { get; set; }

        /// <summary>
        /// Parsing rule that was applied
        /// </summary>
        public string? AppliedRule { get; set; }

        /// <summary>
        /// Creates a new instance of ParsedData
        /// </summary>
        public ParsedData()
        {
        }

        /// <summary>
        /// Creates a new instance of ParsedData with basic information
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="dataSource">Data source</param>
        public ParsedData(string deviceId, string dataSource = "")
        {
            DeviceId = deviceId;
            DataSource = dataSource;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Data mapped and ready for API transmission
    /// </summary>
    public class MappedApiData
    {
        /// <summary>
        /// Name of the API endpoint to send this data to
        /// </summary>
        public string EndpointName { get; set; } = string.Empty;

        /// <summary>
        /// Payload data mapped according to API requirements
        /// </summary>
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp when the mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Unique identifier for this message
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Original parsed data reference
        /// </summary>
        public ParsedData? OriginalParsedData { get; set; }

        /// <summary>
        /// Priority of the message (1 = highest, 10 = lowest)
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Number of retry attempts made
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Maximum number of retries allowed
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Creates a new instance of MappedApiData
        /// </summary>
        public MappedApiData()
        {
        }

        /// <summary>
        /// Creates a new instance of MappedApiData with basic information
        /// </summary>
        /// <param name="endpointName">API endpoint name</param>
        /// <param name="parsedData">Original parsed data</param>
        public MappedApiData(string endpointName, ParsedData? parsedData = null)
        {
            EndpointName = endpointName;
            OriginalParsedData = parsedData;
            CreatedAt = DateTime.Now;
            MessageId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Result of data parsing operation
    /// </summary>
    public class ParsingResult
    {
        /// <summary>
        /// Whether the parsing was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Parsed data (null if parsing failed)
        /// </summary>
        public ParsedData? ParsedData { get; set; }

        /// <summary>
        /// Error message if parsing failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception that occurred during parsing (if any)
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Time taken to parse the data
        /// </summary>
        public TimeSpan ParseDuration { get; set; }

        /// <summary>
        /// Creates a successful parsing result
        /// </summary>
        /// <param name="parsedData">Parsed data</param>
        /// <param name="duration">Time taken to parse</param>
        /// <returns>Successful parsing result</returns>
        public static ParsingResult Success(ParsedData parsedData, TimeSpan duration = default)
        {
            return new ParsingResult
            {
                IsSuccess = true,
                ParsedData = parsedData,
                ParseDuration = duration
            };
        }

        /// <summary>
        /// Creates a failed parsing result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="duration">Time taken before failure</param>
        /// <returns>Failed parsing result</returns>
        public static ParsingResult Failure(string errorMessage, Exception? exception = null, TimeSpan duration = default)
        {
            return new ParsingResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                ParseDuration = duration
            };
        }
    }

    /// <summary>
    /// Result of data mapping operation
    /// </summary>
    public class MappingResult
    {
        /// <summary>
        /// Whether the mapping was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Mapped API data (null if mapping failed)
        /// </summary>
        public MappedApiData? MappedData { get; set; }

        /// <summary>
        /// Error message if mapping failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception that occurred during mapping (if any)
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Time taken to map the data
        /// </summary>
        public TimeSpan MappingDuration { get; set; }

        /// <summary>
        /// Creates a successful mapping result
        /// </summary>
        /// <param name="mappedData">Mapped data</param>
        /// <param name="duration">Time taken to map</param>
        /// <returns>Successful mapping result</returns>
        public static MappingResult Success(MappedApiData mappedData, TimeSpan duration = default)
        {
            return new MappingResult
            {
                IsSuccess = true,
                MappedData = mappedData,
                MappingDuration = duration
            };
        }

        /// <summary>
        /// Creates a failed mapping result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="duration">Time taken before failure</param>
        /// <returns>Failed mapping result</returns>
        public static MappingResult Failure(string errorMessage, Exception? exception = null, TimeSpan duration = default)
        {
            return new MappingResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                MappingDuration = duration
            };
        }
    }

    /// <summary>
    /// Result of validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>Valid validation result</returns>
        public static ValidationResult Valid()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <returns>Invalid validation result</returns>
        public static ValidationResult Invalid(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string>(errors)
            };
        }

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        /// <param name="error">Error message</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Adds a warning to the validation result
        /// </summary>
        /// <param name="warning">Warning message</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}