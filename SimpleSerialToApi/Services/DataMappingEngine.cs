using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Engine for mapping parsed data to API format using configuration rules
    /// </summary>
    public class DataMappingEngine : IDataMappingEngine
    {
        private readonly ILogger<DataMappingEngine> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly ConcurrentDictionary<string, IDataConverter> _converters;
        private readonly ConcurrentDictionary<string, object> _performanceMetrics;

        public DataMappingEngine(
            ILogger<DataMappingEngine> logger,
            IConfigurationService configurationService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _converters = new ConcurrentDictionary<string, IDataConverter>();
            _performanceMetrics = new ConcurrentDictionary<string, object>();

            InitializeDefaultConverters();
            InitializePerformanceMetrics();
        }

        /// <summary>
        /// Map parsed data to API data for a specific endpoint
        /// </summary>
        public MappingResult MapToApiData(ParsedData parsedData, string endpointName)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (parsedData == null)
                {
                    return MappingResult.Failure("Parsed data is null", null, stopwatch.Elapsed);
                }

                if (string.IsNullOrWhiteSpace(endpointName))
                {
                    return MappingResult.Failure("Endpoint name is null or empty", null, stopwatch.Elapsed);
                }

                // Find the API endpoint configuration
                var endpoint = _configurationService.ApiEndpoints.FirstOrDefault(e => 
                    string.Equals(e.Name, endpointName, StringComparison.OrdinalIgnoreCase));

                if (endpoint == null)
                {
                    return MappingResult.Failure($"API endpoint '{endpointName}' not found in configuration", null, stopwatch.Elapsed);
                }

                // Create mapped API data
                var mappedData = new MappedApiData(endpointName, parsedData);

                // Apply mapping rules
                var mappingRules = _configurationService.MappingRules.Where(rule =>
                    parsedData.Fields.ContainsKey(rule.SourceField));

                foreach (var rule in mappingRules)
                {
                    try
                    {
                        var sourceValue = parsedData.Fields[rule.SourceField];
                        var mappedValue = ApplyMappingRule(sourceValue, rule, parsedData);
                        
                        mappedData.Payload[rule.TargetField] = mappedValue;

                        _logger.LogDebug("Mapped field '{SourceField}' -> '{TargetField}': {SourceValue} -> {MappedValue}",
                            rule.SourceField, rule.TargetField, sourceValue, mappedValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to apply mapping rule for field '{SourceField}' -> '{TargetField}'",
                            rule.SourceField, rule.TargetField);

                        // Use default value if mapping fails and field is required
                        if (rule.IsRequired)
                        {
                            mappedData.Payload[rule.TargetField] = GetDefaultValueForType(rule.DataType, rule.DefaultValue);
                        }
                    }
                }

                // Add common fields
                AddCommonFields(mappedData, parsedData);

                // Update performance metrics
                IncrementCounter("MapCount");
                UpdateMetric("LastMapTime", stopwatch.Elapsed.TotalMilliseconds);
                UpdateMetric("AverageMapTime", CalculateAverageMapTime(stopwatch.Elapsed.TotalMilliseconds));

                stopwatch.Stop();

                _logger.LogDebug("Successfully mapped data to endpoint '{EndpointName}', {FieldCount} fields mapped",
                    endpointName, mappedData.Payload.Count);

                return MappingResult.Success(mappedData, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                IncrementCounter("MapErrorCount");
                _logger.LogError(ex, "Error mapping data to endpoint '{EndpointName}'", endpointName);
                return MappingResult.Failure($"Mapping error: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Map multiple parsed data items in batch
        /// </summary>
        public async Task<List<MappingResult>> MapBatchAsync(List<ParsedData> parsedDataList)
        {
            if (parsedDataList == null || parsedDataList.Count == 0)
            {
                return new List<MappingResult>();
            }

            var results = new List<MappingResult>();

            // Determine which endpoint to use (could be configurable logic)
            var primaryEndpoint = _configurationService.ApiEndpoints.FirstOrDefault();
            if (primaryEndpoint == null)
            {
                _logger.LogWarning("No API endpoints configured for batch mapping");
                return parsedDataList.Select(pd => MappingResult.Failure("No API endpoints configured", null)).ToList();
            }

            // Process in parallel for better performance
            var tasks = parsedDataList.Select(async parsedData =>
            {
                return await Task.Run(() => MapToApiData(parsedData, primaryEndpoint.Name));
            });

            var mappingResults = await Task.WhenAll(tasks);
            results.AddRange(mappingResults);

            _logger.LogDebug("Batch mapping completed: {TotalCount} items, {SuccessCount} successful, {FailureCount} failed",
                results.Count, results.Count(r => r.IsSuccess), results.Count(r => !r.IsSuccess));

            return results;
        }

        /// <summary>
        /// Register a data converter for use in mapping
        /// </summary>
        public void RegisterConverter(string name, IDataConverter converter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Converter name cannot be null or empty", nameof(name));
            }

            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            _converters.AddOrUpdate(name, converter, (key, oldValue) =>
            {
                _logger.LogInformation("Overriding converter '{ConverterName}': {OldConverter} -> {NewConverter}",
                    name, oldValue.GetType().Name, converter.GetType().Name);
                return converter;
            });

            _logger.LogInformation("Registered converter '{ConverterName}': {ConverterType}",
                name, converter.GetType().Name);
        }

        /// <summary>
        /// Unregister a data converter
        /// </summary>
        public void UnregisterConverter(string name)
        {
            if (_converters.TryRemove(name, out var removedConverter))
            {
                _logger.LogInformation("Unregistered converter '{ConverterName}': {ConverterType}",
                    name, removedConverter.GetType().Name);
            }
        }

        /// <summary>
        /// Validate a mapping rule configuration
        /// </summary>
        public ValidationResult ValidateMappingRule(MappingRuleConfig rule)
        {
            if (rule == null)
            {
                return ValidationResult.Invalid("Mapping rule is null");
            }

            var result = new ValidationResult { IsValid = true };

            // Validate required fields
            if (string.IsNullOrWhiteSpace(rule.SourceField))
            {
                result.AddError("SourceField is required");
            }

            if (string.IsNullOrWhiteSpace(rule.TargetField))
            {
                result.AddError("TargetField is required");
            }

            // Validate data type
            if (!IsValidDataType(rule.DataType))
            {
                result.AddError($"Invalid data type: {rule.DataType}");
            }

            // Validate converter if specified
            if (!string.IsNullOrWhiteSpace(rule.Converter))
            {
                if (!_converters.ContainsKey(rule.Converter))
                {
                    result.AddError($"Converter '{rule.Converter}' is not registered");
                }
            }

            return result;
        }

        /// <summary>
        /// Get all registered converters
        /// </summary>
        public Dictionary<string, Type> GetRegisteredConverters()
        {
            return _converters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetType());
        }

        /// <summary>
        /// Get performance metrics for the mapping engine
        /// </summary>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var metrics = new Dictionary<string, object>(_performanceMetrics)
            {
                ["RegisteredConverterCount"] = _converters.Count
            };
            return metrics;
        }

        /// <summary>
        /// Apply a mapping rule to transform a source value
        /// </summary>
        private object ApplyMappingRule(object sourceValue, MappingRuleConfig rule, ParsedData context)
        {
            object mappedValue = sourceValue;

            // Apply converter if specified
            if (!string.IsNullOrWhiteSpace(rule.Converter) && _converters.TryGetValue(rule.Converter, out var converter))
            {
                var conversionContext = new ConversionContext
                {
                    SourceField = rule.SourceField,
                    TargetField = rule.TargetField,
                    ParsedData = context
                };

                // Parse converter parameters from configuration (could be extended)
                // For now, assume parameters are in the format "key1=value1;key2=value2"
                // This could be enhanced to support more complex parameter formats

                mappedValue = converter.Convert(sourceValue, conversionContext);
            }

            // Convert to target data type if different from current
            mappedValue = ConvertToDataType(mappedValue, rule.DataType);

            return mappedValue;
        }

        /// <summary>
        /// Add common fields to mapped data
        /// </summary>
        private void AddCommonFields(MappedApiData mappedData, ParsedData parsedData)
        {
            // Add timestamp if not already present
            if (!mappedData.Payload.ContainsKey("timestamp"))
            {
                mappedData.Payload["timestamp"] = parsedData.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }

            // Add device ID if not already present
            if (!mappedData.Payload.ContainsKey("deviceId") && !string.IsNullOrWhiteSpace(parsedData.DeviceId))
            {
                mappedData.Payload["deviceId"] = parsedData.DeviceId;
            }

            // Add data source if not already present
            if (!mappedData.Payload.ContainsKey("dataSource") && !string.IsNullOrWhiteSpace(parsedData.DataSource))
            {
                mappedData.Payload["dataSource"] = parsedData.DataSource;
            }
        }

        /// <summary>
        /// Convert value to specified data type
        /// </summary>
        private object ConvertToDataType(object value, string dataType)
        {
            if (value == null)
            {
                return GetDefaultValueForType(dataType, null);
            }

            try
            {
                return dataType.ToLowerInvariant() switch
                {
                    "string" => value.ToString() ?? string.Empty,
                    "int" or "integer" => Convert.ToInt32(value),
                    "long" => Convert.ToInt64(value),
                    "decimal" => Convert.ToDecimal(value),
                    "double" => Convert.ToDouble(value),
                    "float" => Convert.ToSingle(value),
                    "bool" or "boolean" => Convert.ToBoolean(value),
                    "datetime" => Convert.ToDateTime(value),
                    _ => value // Return as-is if type not recognized
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert value '{Value}' to type '{DataType}'", value, dataType);
                return GetDefaultValueForType(dataType, null);
            }
        }

        /// <summary>
        /// Get default value for a data type
        /// </summary>
        private object GetDefaultValueForType(string dataType, string? configuredDefault)
        {
            // Use configured default value if provided
            if (!string.IsNullOrWhiteSpace(configuredDefault))
            {
                try
                {
                    return ConvertToDataType(configuredDefault, dataType);
                }
                catch
                {
                    // Fall through to type defaults
                }
            }

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
        /// Initialize default converters
        /// </summary>
        private void InitializeDefaultConverters()
        {
            try
            {
                RegisterConverter("TemperatureConverter", new Services.Converters.TemperatureConverter());
                RegisterConverter("DateTimeConverter", new Services.Converters.DateTimeConverter());
                RegisterConverter("NumericConverter", new Services.Converters.NumericConverter());
                RegisterConverter("StringConverter", new Services.Converters.StringConverter());

                _logger.LogInformation("Default data converters initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize default converters");
            }
        }

        /// <summary>
        /// Initialize performance metrics
        /// </summary>
        private void InitializePerformanceMetrics()
        {
            _performanceMetrics.TryAdd("MapCount", 0L);
            _performanceMetrics.TryAdd("MapErrorCount", 0L);
            _performanceMetrics.TryAdd("LastMapTime", 0.0);
            _performanceMetrics.TryAdd("AverageMapTime", 0.0);
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
        }

        /// <summary>
        /// Calculate average mapping time
        /// </summary>
        private double CalculateAverageMapTime(double newTime)
        {
            var mapCount = (long)_performanceMetrics.GetValueOrDefault("MapCount", 0L);
            var currentAverage = (double)_performanceMetrics.GetValueOrDefault("AverageMapTime", 0.0);

            if (mapCount <= 1)
            {
                return newTime;
            }

            // Calculate running average
            return ((currentAverage * (mapCount - 1)) + newTime) / mapCount;
        }
    }
}