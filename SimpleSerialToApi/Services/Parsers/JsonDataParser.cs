using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    /// Parser for JSON-formatted serial data
    /// </summary>
    public class JsonDataParser : IDataParser
    {
        private readonly ILogger<JsonDataParser>? _logger;
        private readonly ConcurrentDictionary<string, object> _performanceMetrics;

        public string SupportedFormat => "JSON";

        public JsonDataParser(ILogger<JsonDataParser>? logger = null)
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

                // Convert bytes to JSON string
                var jsonString = Encoding.UTF8.GetString(rawData.Data).Trim();
                if (string.IsNullOrEmpty(jsonString))
                {
                    return ParsingResult.Failure("JSON string is empty after conversion", null, stopwatch.Elapsed);
                }

                // Parse JSON
                var jsonObject = JObject.Parse(jsonString);

                // Create parsed data
                var parsedData = new ParsedData(rawData.DeviceId, rawData.PortName)
                {
                    OriginalData = rawData,
                    AppliedRule = rule.Name,
                    Timestamp = rawData.ReceivedTime
                };

                // Extract fields using JSON paths
                for (int i = 0; i < rule.Fields.Count; i++)
                {
                    var fieldName = rule.Fields[i];
                    // In JSON parsing, the pattern can contain JSON paths separated by comma
                    // Or we can use field names as JSON paths directly
                    var jsonPath = i < rule.Pattern.Split(',').Length ? 
                        rule.Pattern.Split(',')[i].Trim() : fieldName;

                    var dataType = i < rule.DataTypes.Count ? rule.DataTypes[i] : "string";

                    try
                    {
                        var token = jsonObject.SelectToken(jsonPath);
                        if (token != null)
                        {
                            var value = ConvertJsonValue(token, dataType);
                            parsedData.Fields[fieldName] = value;
                        }
                        else
                        {
                            _logger?.LogWarning("JSON path '{JsonPath}' not found in data", jsonPath);
                            parsedData.Fields[fieldName] = GetDefaultValue(dataType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to extract field '{FieldName}' using path '{JsonPath}'", fieldName, jsonPath);
                        parsedData.Fields[fieldName] = GetDefaultValue(dataType);
                    }
                }

                // Update performance metrics
                IncrementCounter("ParseCount");
                UpdateMetric("LastParseTime", stopwatch.Elapsed.TotalMilliseconds);

                stopwatch.Stop();

                _logger?.LogDebug("Successfully parsed JSON data with rule '{RuleName}', extracted {FieldCount} fields",
                    rule.Name, parsedData.Fields.Count);

                return ParsingResult.Success(parsedData, stopwatch.Elapsed);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                IncrementCounter("ParseErrorCount");
                _logger?.LogError(ex, "JSON parse error with rule '{RuleName}'", rule?.Name);
                return ParsingResult.Failure($"JSON parse error: {ex.Message}", ex, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                IncrementCounter("ParseErrorCount");
                _logger?.LogError(ex, "Error parsing JSON data with rule '{RuleName}'", rule?.Name);
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

            try
            {
                var jsonString = Encoding.UTF8.GetString(rawData.Data).Trim();
                if (string.IsNullOrEmpty(jsonString))
                {
                    return false;
                }

                // Try to parse as JSON to validate
                JToken.Parse(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
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

            // Validate fields
            if (rule.Fields.Count == 0)
            {
                result.AddError("At least one field must be defined");
            }

            // For JSON, pattern can be optional (field names used as paths) or contain JSON paths
            if (!string.IsNullOrWhiteSpace(rule.Pattern))
            {
                var paths = rule.Pattern.Split(',');
                if (paths.Length != rule.Fields.Count)
                {
                    result.AddWarning($"Pattern has {paths.Length} JSON paths but {rule.Fields.Count} fields are defined");
                }
            }

            return result;
        }

        public Dictionary<string, object> GetPerformanceMetrics()
        {
            return new Dictionary<string, object>(_performanceMetrics);
        }

        private object ConvertJsonValue(JToken token, string dataType)
        {
            try
            {
                return dataType.ToLowerInvariant() switch
                {
                    "string" => token.Value<string>() ?? string.Empty,
                    "int" or "integer" => token.Value<int>(),
                    "long" => token.Value<long>(),
                    "decimal" => token.Value<decimal>(),
                    "double" => token.Value<double>(),
                    "float" => token.Value<float>(),
                    "bool" or "boolean" => token.Value<bool>(),
                    "datetime" => token.Value<DateTime>(),
                    _ => token.Value<string>() ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to convert JSON token to type '{DataType}'", dataType);
                return GetDefaultValue(dataType);
            }
        }

        private object GetDefaultValue(string dataType)
        {
            return dataType.ToLowerInvariant() switch
            {
                "string" => string.Empty,
                "int" or "integer" => 0,
                "long" => 0L,
                "decimal" => 0m,
                "double" => 0.0,
                "float" => 0.0f,
                "bool" or "boolean" => false,
                "datetime" => DateTime.MinValue,
                _ => string.Empty
            };
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