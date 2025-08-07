using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Parser for binary serial data
    /// </summary>
    public class BinaryDataParser : IDataParser
    {
        private readonly ILogger<BinaryDataParser>? _logger;
        private readonly ConcurrentDictionary<string, object> _performanceMetrics;

        public string SupportedFormat => "BINARY";

        public BinaryDataParser(ILogger<BinaryDataParser>? logger = null)
        {
            _logger = logger;
            _performanceMetrics = new ConcurrentDictionary<string, object>();
            InitializePerformanceMetrics();
        }

        public ParsingResult Parse(RawSerialData rawData, ParsingRule rule)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (rawData?.Data == null || rawData.Data.Length == 0)
                {
                    return ParsingResult.Failure("Raw data is null or empty", null, stopwatch.Elapsed);
                }

                if (rule == null)
                {
                    return ParsingResult.Failure("Parsing rule is null", null, stopwatch.Elapsed);
                }

                // Create parsed data
                var parsedData = new ParsedData(rawData.DeviceId, rawData.PortName)
                {
                    OriginalData = rawData,
                    AppliedRule = rule.Name,
                    Timestamp = rawData.ReceivedTime
                };

                // For binary data, pattern defines field layout: "position:length:type"
                // Example: "0:1:byte,1:2:short,3:4:int,7:4:float"
                var fieldSpecs = rule.Pattern.Split(',');
                
                for (int i = 0; i < Math.Min(fieldSpecs.Length, rule.Fields.Count); i++)
                {
                    var spec = fieldSpecs[i].Trim();
                    var parts = spec.Split(':');
                    
                    if (parts.Length >= 2 && 
                        int.TryParse(parts[0], out var position) && 
                        int.TryParse(parts[1], out var length))
                    {
                        var fieldName = rule.Fields[i];
                        var dataType = parts.Length > 2 ? parts[2] : 
                                      (i < rule.DataTypes.Count ? rule.DataTypes[i] : "byte");
                        
                        var fieldValue = ExtractBinaryField(rawData.Data, position, length, dataType);
                        parsedData.Fields[fieldName] = fieldValue;
                    }
                    else
                    {
                        _logger?.LogWarning("Invalid binary field specification: {Spec}", spec);
                        if (i < rule.Fields.Count)
                        {
                            parsedData.Fields[rule.Fields[i]] = 0;
                        }
                    }
                }

                // Update performance metrics
                IncrementCounter("ParseCount");
                UpdateMetric("LastParseTime", stopwatch.Elapsed.TotalMilliseconds);

                stopwatch.Stop();

                _logger?.LogDebug("Successfully parsed binary data with rule '{RuleName}', extracted {FieldCount} fields",
                    rule.Name, parsedData.Fields.Count);

                return ParsingResult.Success(parsedData, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                IncrementCounter("ParseErrorCount");
                _logger?.LogError(ex, "Error parsing binary data with rule '{RuleName}'", rule?.Name);
                return ParsingResult.Failure($"Parse error: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        public bool CanParse(RawSerialData rawData)
        {
            if (rawData?.Data == null || rawData.Data.Length == 0)
            {
                return false;
            }

            // Check if format matches
            if (!string.IsNullOrEmpty(rawData.DataFormat) && 
                !string.Equals(rawData.DataFormat, SupportedFormat, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Binary data can always be parsed, so return true if format matches
            return true;
        }

        public ValidationResult ValidateRule(ParsingRule rule)
        {
            if (rule == null)
            {
                return ValidationResult.Invalid("Parsing rule is null");
            }

            var result = new ValidationResult { IsValid = true };

            // Check if data format matches
            if (!string.IsNullOrEmpty(rule.DataFormat) && 
                !string.Equals(rule.DataFormat, SupportedFormat, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError($"Data format '{rule.DataFormat}' is not supported by {SupportedFormat} parser");
            }

            // Validate pattern format
            if (string.IsNullOrWhiteSpace(rule.Pattern))
            {
                result.AddError("Pattern is required for binary data");
            }
            else
            {
                var fieldSpecs = rule.Pattern.Split(',');
                foreach (var spec in fieldSpecs)
                {
                    var parts = spec.Trim().Split(':');
                    if (parts.Length < 2 || 
                        !int.TryParse(parts[0], out _) || 
                        !int.TryParse(parts[1], out _))
                    {
                        result.AddError($"Invalid binary field specification: '{spec}'. Expected format: 'position:length' or 'position:length:type'");
                    }

                    if (parts.Length > 2)
                    {
                        var dataType = parts[2].ToLowerInvariant();
                        var validTypes = new[] { "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double" };
                        if (!Array.Exists(validTypes, t => t == dataType))
                        {
                            result.AddError($"Invalid binary data type: '{dataType}'");
                        }
                    }
                }
            }

            return result;
        }

        public Dictionary<string, object> GetPerformanceMetrics()
        {
            return new Dictionary<string, object>(_performanceMetrics);
        }

        private object ExtractBinaryField(byte[] data, int position, int length, string dataType)
        {
            try
            {
                // Check bounds
                if (position < 0 || position >= data.Length || 
                    position + length > data.Length || length <= 0)
                {
                    _logger?.LogWarning("Invalid binary field position/length: pos={Position}, len={Length}, dataLen={DataLength}", 
                        position, length, data.Length);
                    return 0;
                }

                // Extract bytes
                var fieldBytes = new byte[length];
                Array.Copy(data, position, fieldBytes, 0, length);

                // Convert based on data type
                return dataType.ToLowerInvariant() switch
                {
                    "byte" => fieldBytes[0],
                    "short" => length >= 2 ? BitConverter.ToInt16(fieldBytes, 0) : fieldBytes[0],
                    "ushort" => length >= 2 ? BitConverter.ToUInt16(fieldBytes, 0) : fieldBytes[0],
                    "int" => length >= 4 ? BitConverter.ToInt32(fieldBytes, 0) : 
                             length >= 2 ? BitConverter.ToInt16(fieldBytes, 0) : fieldBytes[0],
                    "uint" => length >= 4 ? BitConverter.ToUInt32(fieldBytes, 0) : 
                              length >= 2 ? BitConverter.ToUInt16(fieldBytes, 0) : fieldBytes[0],
                    "long" => length >= 8 ? BitConverter.ToInt64(fieldBytes, 0) : 
                              length >= 4 ? BitConverter.ToInt32(fieldBytes, 0) : 
                              length >= 2 ? BitConverter.ToInt16(fieldBytes, 0) : fieldBytes[0],
                    "ulong" => length >= 8 ? BitConverter.ToUInt64(fieldBytes, 0) : 
                               length >= 4 ? BitConverter.ToUInt32(fieldBytes, 0) : 
                               length >= 2 ? BitConverter.ToUInt16(fieldBytes, 0) : fieldBytes[0],
                    "float" => length >= 4 ? BitConverter.ToSingle(fieldBytes, 0) : 0.0f,
                    "double" => length >= 8 ? BitConverter.ToDouble(fieldBytes, 0) : 
                                length >= 4 ? BitConverter.ToSingle(fieldBytes, 0) : 0.0,
                    _ => fieldBytes[0] // Default to byte
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to extract binary field at position {Position}, length {Length}", position, length);
                return 0;
            }
        }

        private void InitializePerformanceMetrics()
        {
            _performanceMetrics.TryAdd("ParseCount", 0L);
            _performanceMetrics.TryAdd("ParseErrorCount", 0L);
            _performanceMetrics.TryAdd("LastParseTime", 0.0);
        }

        private void IncrementCounter(string metricName)
        {
            _performanceMetrics.AddOrUpdate(metricName, 1L, (key, oldValue) => (long)oldValue + 1);
        }

        private void UpdateMetric(string metricName, object value)
        {
            _performanceMetrics.AddOrUpdate(metricName, value, (key, oldValue) => value);
        }
    }
}