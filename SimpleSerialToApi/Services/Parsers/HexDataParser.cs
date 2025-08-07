using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Parser for hexadecimal serial data
    /// </summary>
    public class HexDataParser : IDataParser
    {
        private readonly ILogger<HexDataParser>? _logger;
        private readonly ConcurrentDictionary<string, object> _performanceMetrics;

        public string SupportedFormat => "HEX";

        public HexDataParser(ILogger<HexDataParser>? logger = null)
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

                // Convert bytes to hex string
                var hexString = Convert.ToHexString(rawData.Data);
                
                _logger?.LogDebug("Parsing HEX data: {HexData}", hexString);

                // Create parsed data
                var parsedData = new ParsedData(rawData.DeviceId, rawData.PortName)
                {
                    OriginalData = rawData,
                    AppliedRule = rule.Name,
                    Timestamp = rawData.ReceivedTime
                };

                // For HEX data, we'll extract fields based on byte positions
                // Rule pattern should be in format: "position:length,position:length"
                // Example: "0:2,2:4,6:2" means extract 2 bytes at pos 0, 4 bytes at pos 2, 2 bytes at pos 6
                var fieldSpecs = rule.Pattern.Split(',');
                
                for (int i = 0; i < Math.Min(fieldSpecs.Length, rule.Fields.Count); i++)
                {
                    var spec = fieldSpecs[i].Trim();
                    var parts = spec.Split(':');
                    
                    if (parts.Length == 2 && 
                        int.TryParse(parts[0], out var position) && 
                        int.TryParse(parts[1], out var length))
                    {
                        var fieldName = rule.Fields[i];
                        var dataType = i < rule.DataTypes.Count ? rule.DataTypes[i] : "hex";
                        
                        var fieldValue = ExtractHexField(rawData.Data, position, length, dataType);
                        parsedData.Fields[fieldName] = fieldValue;
                    }
                    else
                    {
                        _logger?.LogWarning("Invalid field specification: {Spec}", spec);
                        if (i < rule.Fields.Count)
                        {
                            parsedData.Fields[rule.Fields[i]] = string.Empty;
                        }
                    }
                }

                // Update performance metrics
                IncrementCounter("ParseCount");
                UpdateMetric("LastParseTime", stopwatch.Elapsed.TotalMilliseconds);

                stopwatch.Stop();

                _logger?.LogDebug("Successfully parsed HEX data with rule '{RuleName}', extracted {FieldCount} fields",
                    rule.Name, parsedData.Fields.Count);

                return ParsingResult.Success(parsedData, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                IncrementCounter("ParseErrorCount");
                _logger?.LogError(ex, "Error parsing HEX data with rule '{RuleName}'", rule?.Name);
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
                result.AddError("Pattern is required");
            }
            else
            {
                var fieldSpecs = rule.Pattern.Split(',');
                foreach (var spec in fieldSpecs)
                {
                    var parts = spec.Trim().Split(':');
                    if (parts.Length != 2 || 
                        !int.TryParse(parts[0], out _) || 
                        !int.TryParse(parts[1], out _))
                    {
                        result.AddError($"Invalid field specification format: '{spec}'. Expected format: 'position:length'");
                    }
                }
            }

            return result;
        }

        public Dictionary<string, object> GetPerformanceMetrics()
        {
            return new Dictionary<string, object>(_performanceMetrics);
        }

        private object ExtractHexField(byte[] data, int position, int length, string dataType)
        {
            try
            {
                // Check bounds
                if (position < 0 || position >= data.Length || 
                    position + length > data.Length || length <= 0)
                {
                    _logger?.LogWarning("Invalid field position/length: pos={Position}, len={Length}, dataLen={DataLength}", 
                        position, length, data.Length);
                    return string.Empty;
                }

                // Extract bytes
                var fieldBytes = new byte[length];
                Array.Copy(data, position, fieldBytes, 0, length);

                // Convert based on data type
                return dataType.ToLowerInvariant() switch
                {
                    "hex" => Convert.ToHexString(fieldBytes),
                    "int" or "integer" => length switch
                    {
                        1 => fieldBytes[0],
                        2 => BitConverter.ToInt16(fieldBytes),
                        4 => BitConverter.ToInt32(fieldBytes),
                        _ => Convert.ToHexString(fieldBytes)
                    },
                    "uint" => length switch
                    {
                        1 => fieldBytes[0],
                        2 => BitConverter.ToUInt16(fieldBytes),
                        4 => BitConverter.ToUInt32(fieldBytes),
                        _ => Convert.ToHexString(fieldBytes)
                    },
                    "float" => length == 4 ? BitConverter.ToSingle(fieldBytes) : Convert.ToHexString(fieldBytes),
                    "double" => length == 8 ? BitConverter.ToDouble(fieldBytes) : Convert.ToHexString(fieldBytes),
                    "ascii" => Encoding.ASCII.GetString(fieldBytes).TrimEnd('\0'),
                    _ => Convert.ToHexString(fieldBytes)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to extract HEX field at position {Position}, length {Length}", position, length);
                return string.Empty;
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