using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Parser for text-based serial data
    /// </summary>
    public class TextDataParser : IDataParser
    {
        private readonly ILogger<TextDataParser>? _logger;
        private readonly ConcurrentDictionary<string, Regex> _compiledRegexCache;
        private readonly ConcurrentDictionary<string, object> _performanceMetrics;

        public string SupportedFormat => "TEXT";

        public TextDataParser(ILogger<TextDataParser>? logger = null)
        {
            _logger = logger;
            _compiledRegexCache = new ConcurrentDictionary<string, Regex>();
            _performanceMetrics = new ConcurrentDictionary<string, object>();

            InitializePerformanceMetrics();
        }

        /// <summary>
        /// Parse raw serial data using the specified rule
        /// </summary>
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

                // Convert bytes to text
                var textData = Encoding.UTF8.GetString(rawData.Data).Trim();
                if (string.IsNullOrEmpty(textData))
                {
                    return ParsingResult.Failure("Text data is empty after conversion", null, stopwatch.Elapsed);
                }

                // Get compiled regex for the pattern
                var regex = GetCompiledRegex(rule.Pattern);
                if (regex == null)
                {
                    return ParsingResult.Failure($"Invalid regex pattern: {rule.Pattern}", null, stopwatch.Elapsed);
                }

                // Apply regex pattern
                var match = regex.Match(textData);
                if (!match.Success)
                {
                    _logger?.LogDebug("Pattern '{Pattern}' did not match text data: '{TextData}'", rule.Pattern, textData);
                    return ParsingResult.Failure($"Pattern did not match data: {rule.Pattern}", null, stopwatch.Elapsed);
                }

                // Extract field values
                var parsedData = new ParsedData(rawData.DeviceId, rawData.PortName)
                {
                    OriginalData = rawData,
                    AppliedRule = rule.Name,
                    Timestamp = rawData.ReceivedTime
                };

                // Extract groups from regex match
                var fieldCount = Math.Min(rule.Fields.Count, rule.DataTypes.Count);
                for (int i = 0; i < fieldCount; i++)
                {
                    var fieldName = rule.Fields[i];
                    var dataType = rule.DataTypes[i];

                    // Get the captured group (group 0 is the full match, so start from group 1)
                    if (i + 1 < match.Groups.Count)
                    {
                        var groupValue = match.Groups[i + 1].Value;
                        var convertedValue = ConvertToDataType(groupValue, dataType);
                        parsedData.Fields[fieldName] = convertedValue;
                    }
                    else
                    {
                        _logger?.LogWarning("No regex group found for field {FieldName} at index {Index}", fieldName, i);
                        parsedData.Fields[fieldName] = GetDefaultValue(dataType);
                    }
                }

                // Update performance metrics
                IncrementCounter("ParseCount");
                UpdateMetric("LastParseTime", stopwatch.Elapsed.TotalMilliseconds);
                UpdateMetric("AverageParseTime", CalculateAverageParseTime(stopwatch.Elapsed.TotalMilliseconds));

                stopwatch.Stop();

                _logger?.LogDebug("Successfully parsed text data with rule '{RuleName}', extracted {FieldCount} fields",
                    rule.Name, parsedData.Fields.Count);

                return ParsingResult.Success(parsedData, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                IncrementCounter("ParseErrorCount");
                _logger?.LogError(ex, "Error parsing text data with rule '{RuleName}'", rule?.Name);
                return ParsingResult.Failure($"Parse error: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Check if this parser can parse the given raw data
        /// </summary>
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
                // Try to convert to UTF-8 text and check if it's valid
                var text = Encoding.UTF8.GetString(rawData.Data);
                
                // Check for common non-text indicators
                // If the text contains many null characters or non-printable characters, it's likely binary
                int nullCount = 0;
                int nonPrintableCount = 0;
                
                foreach (char c in text)
                {
                    if (c == '\0') nullCount++;
                    else if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t') nonPrintableCount++;
                }

                // If more than 10% of characters are null or non-printable, consider it non-text
                double threshold = text.Length * 0.1;
                if (nullCount > threshold || nonPrintableCount > threshold)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate that a parsing rule is correct for this parser
        /// </summary>
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

            // Validate regex pattern
            if (string.IsNullOrWhiteSpace(rule.Pattern))
            {
                result.AddError("Pattern is required");
            }
            else
            {
                try
                {
                    var regex = new Regex(rule.Pattern, RegexOptions.Compiled);
                    
                    // Count capture groups
                    var groupCount = regex.GetGroupNumbers().Length - 1; // Subtract 1 for group 0 (full match)
                    
                    if (groupCount != rule.Fields.Count)
                    {
                        result.AddWarning($"Regex pattern has {groupCount} capture groups but {rule.Fields.Count} fields are defined");
                    }
                }
                catch (ArgumentException ex)
                {
                    result.AddError($"Invalid regex pattern: {ex.Message}");
                }
            }

            // Validate fields
            if (rule.Fields.Count == 0)
            {
                result.AddError("At least one field must be defined");
            }

            if (rule.Fields.Count != rule.DataTypes.Count)
            {
                result.AddError($"Field count ({rule.Fields.Count}) does not match data type count ({rule.DataTypes.Count})");
            }

            // Validate data types
            foreach (var dataType in rule.DataTypes)
            {
                if (!IsValidDataType(dataType))
                {
                    result.AddError($"Invalid data type: {dataType}");
                }
            }

            return result;
        }

        /// <summary>
        /// Get performance metrics for this parser
        /// </summary>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            return new Dictionary<string, object>(_performanceMetrics);
        }

        /// <summary>
        /// Get compiled regex for the given pattern with caching
        /// </summary>
        private Regex? GetCompiledRegex(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            return _compiledRegexCache.GetOrAdd(pattern, p =>
            {
                try
                {
                    return new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
                catch (ArgumentException ex)
                {
                    _logger?.LogError(ex, "Failed to compile regex pattern: {Pattern}", p);
                    throw;
                }
            });
        }

        /// <summary>
        /// Convert string value to the specified data type
        /// </summary>
        private object ConvertToDataType(string value, string dataType)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(dataType);
            }

            try
            {
                return dataType.ToLowerInvariant() switch
                {
                    "string" => value,
                    "int" or "integer" => int.Parse(value, CultureInfo.InvariantCulture),
                    "long" => long.Parse(value, CultureInfo.InvariantCulture),
                    "decimal" => decimal.Parse(value, CultureInfo.InvariantCulture),
                    "double" => double.Parse(value, CultureInfo.InvariantCulture),
                    "float" => float.Parse(value, CultureInfo.InvariantCulture),
                    "bool" or "boolean" => bool.Parse(value),
                    "datetime" => DateTime.Parse(value, CultureInfo.InvariantCulture),
                    _ => value // Default to string if type not recognized
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to convert value '{Value}' to type '{DataType}', using default", value, dataType);
                return GetDefaultValue(dataType);
            }
        }

        /// <summary>
        /// Get default value for a data type
        /// </summary>
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

        /// <summary>
        /// Check if a data type is valid
        /// </summary>
        private bool IsValidDataType(string dataType)
        {
            var validTypes = new[] { "string", "int", "integer", "long", "decimal", "double", "float", "bool", "boolean", "datetime" };
            return validTypes.Contains(dataType.ToLowerInvariant());
        }

        /// <summary>
        /// Initialize performance metrics
        /// </summary>
        private void InitializePerformanceMetrics()
        {
            _performanceMetrics.TryAdd("ParseCount", 0L);
            _performanceMetrics.TryAdd("ParseErrorCount", 0L);
            _performanceMetrics.TryAdd("LastParseTime", 0.0);
            _performanceMetrics.TryAdd("AverageParseTime", 0.0);
            _performanceMetrics.TryAdd("RegexCacheSize", 0);
        }

        /// <summary>
        /// Increment a counter metric
        /// </summary>
        private void IncrementCounter(string metricName)
        {
            _performanceMetrics.AddOrUpdate(metricName, 1L, (key, oldValue) => (long)oldValue + 1);
        }

        /// <summary>
        /// Update a metric value
        /// </summary>
        private void UpdateMetric(string metricName, object value)
        {
            _performanceMetrics.AddOrUpdate(metricName, value, (key, oldValue) => value);
            
            // Update regex cache size
            if (metricName == "LastParseTime")
            {
                _performanceMetrics.AddOrUpdate("RegexCacheSize", _compiledRegexCache.Count, (key, oldValue) => _compiledRegexCache.Count);
            }
        }

        /// <summary>
        /// Calculate average parse time
        /// </summary>
        private double CalculateAverageParseTime(double newTime)
        {
            var parseCount = (long)_performanceMetrics.GetValueOrDefault("ParseCount", 0L);
            var currentAverage = (double)_performanceMetrics.GetValueOrDefault("AverageParseTime", 0.0);

            if (parseCount <= 1)
            {
                return newTime;
            }

            // Calculate running average
            return ((currentAverage * (parseCount - 1)) + newTime) / parseCount;
        }
    }
}