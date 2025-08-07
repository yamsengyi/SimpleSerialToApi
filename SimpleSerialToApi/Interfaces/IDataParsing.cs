using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Context information for data conversion
    /// </summary>
    public class ConversionContext
    {
        /// <summary>
        /// Source field name
        /// </summary>
        public string SourceField { get; set; } = string.Empty;

        /// <summary>
        /// Target field name
        /// </summary>
        public string TargetField { get; set; } = string.Empty;

        /// <summary>
        /// Additional conversion parameters
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Original parsed data
        /// </summary>
        public ParsedData? ParsedData { get; set; }
    }

    /// <summary>
    /// Configuration for parsing rules
    /// </summary>
    public class ParsingRule
    {
        /// <summary>
        /// Name of the parsing rule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Pattern to match against the data
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Field names to extract
        /// </summary>
        public List<string> Fields { get; set; } = new List<string>();

        /// <summary>
        /// Data types for each field
        /// </summary>
        public List<string> DataTypes { get; set; } = new List<string>();

        /// <summary>
        /// Data format this rule applies to (HEX, TEXT, JSON, BINARY)
        /// </summary>
        public string DataFormat { get; set; } = "TEXT";

        /// <summary>
        /// Priority of this rule (higher number = higher priority)
        /// </summary>
        public int Priority { get; set; } = 1;
    }

    /// <summary>
    /// Interface for data parsers that convert raw serial data to structured data
    /// </summary>
    public interface IDataParser
    {
        /// <summary>
        /// Data format supported by this parser (HEX, TEXT, JSON, BINARY)
        /// </summary>
        string SupportedFormat { get; }

        /// <summary>
        /// Parse raw serial data using the specified rule
        /// </summary>
        /// <param name="rawData">Raw serial data to parse</param>
        /// <param name="rule">Parsing rule to apply</param>
        /// <returns>Parsing result</returns>
        ParsingResult Parse(RawSerialData rawData, ParsingRule rule);

        /// <summary>
        /// Check if this parser can parse the given raw data
        /// </summary>
        /// <param name="rawData">Raw data to check</param>
        /// <returns>True if parser can handle this data</returns>
        bool CanParse(RawSerialData rawData);

        /// <summary>
        /// Validate that a parsing rule is correct for this parser
        /// </summary>
        /// <param name="rule">Rule to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateRule(ParsingRule rule);

        /// <summary>
        /// Get performance metrics for this parser
        /// </summary>
        /// <returns>Performance information</returns>
        Dictionary<string, object> GetPerformanceMetrics();
    }

    /// <summary>
    /// Interface for data mapping engine that maps parsed data to API format
    /// </summary>
    public interface IDataMappingEngine
    {
        /// <summary>
        /// Map parsed data to API data for a specific endpoint
        /// </summary>
        /// <param name="parsedData">Parsed data to map</param>
        /// <param name="endpointName">Target API endpoint name</param>
        /// <returns>Mapping result</returns>
        MappingResult MapToApiData(ParsedData parsedData, string endpointName);

        /// <summary>
        /// Map multiple parsed data items in batch
        /// </summary>
        /// <param name="parsedDataList">List of parsed data to map</param>
        /// <returns>List of mapping results</returns>
        Task<List<MappingResult>> MapBatchAsync(List<ParsedData> parsedDataList);

        /// <summary>
        /// Register a data converter for use in mapping
        /// </summary>
        /// <param name="name">Converter name</param>
        /// <param name="converter">Converter instance</param>
        void RegisterConverter(string name, IDataConverter converter);

        /// <summary>
        /// Unregister a data converter
        /// </summary>
        /// <param name="name">Converter name to remove</param>
        void UnregisterConverter(string name);

        /// <summary>
        /// Validate a mapping rule configuration
        /// </summary>
        /// <param name="rule">Mapping rule to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateMappingRule(MappingRuleConfig rule);

        /// <summary>
        /// Get all registered converters
        /// </summary>
        /// <returns>Dictionary of converter names and types</returns>
        Dictionary<string, Type> GetRegisteredConverters();

        /// <summary>
        /// Get performance metrics for the mapping engine
        /// </summary>
        /// <returns>Performance information</returns>
        Dictionary<string, object> GetPerformanceMetrics();
    }

    /// <summary>
    /// Interface for data converters that transform data types/values
    /// </summary>
    public interface IDataConverter
    {
        /// <summary>
        /// Name of the converter
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Convert input data to the target format
        /// </summary>
        /// <param name="input">Input value to convert</param>
        /// <param name="context">Conversion context</param>
        /// <returns>Converted value</returns>
        object Convert(object input, ConversionContext context);

        /// <summary>
        /// Check if this converter can convert between the specified types
        /// </summary>
        /// <param name="sourceType">Source data type</param>
        /// <param name="targetType">Target data type</param>
        /// <returns>True if conversion is supported</returns>
        bool CanConvert(Type sourceType, Type targetType);

        /// <summary>
        /// Get all supported data types for this converter
        /// </summary>
        Type[] SupportedTypes { get; }

        /// <summary>
        /// Validate conversion parameters
        /// </summary>
        /// <param name="context">Conversion context with parameters</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateParameters(ConversionContext context);
    }

    /// <summary>
    /// Interface for creating appropriate data parsers
    /// </summary>
    public interface IDataParserFactory
    {
        /// <summary>
        /// Create a parser for the specified data format
        /// </summary>
        /// <param name="dataFormat">Data format (HEX, TEXT, JSON, BINARY)</param>
        /// <returns>Data parser instance or null if format not supported</returns>
        IDataParser? CreateParser(string dataFormat);

        /// <summary>
        /// Get a parser that can handle the given raw data
        /// </summary>
        /// <param name="rawData">Raw data to parse</param>
        /// <returns>Suitable parser or null if none found</returns>
        IDataParser? GetParserForData(RawSerialData rawData);

        /// <summary>
        /// Register a new parser type
        /// </summary>
        /// <param name="dataFormat">Data format handled by the parser</param>
        /// <param name="parserType">Parser type to register</param>
        void RegisterParser(string dataFormat, Type parserType);

        /// <summary>
        /// Get all supported data formats
        /// </summary>
        /// <returns>List of supported formats</returns>
        string[] GetSupportedFormats();
    }

    /// <summary>
    /// Interface for managing parsing rules
    /// </summary>
    public interface IParsingRuleManager
    {
        /// <summary>
        /// Get all parsing rules
        /// </summary>
        /// <returns>List of parsing rules</returns>
        List<ParsingRule> GetAllRules();

        /// <summary>
        /// Get parsing rules for a specific data format
        /// </summary>
        /// <param name="dataFormat">Data format</param>
        /// <returns>List of matching rules</returns>
        List<ParsingRule> GetRulesForFormat(string dataFormat);

        /// <summary>
        /// Find the best matching rule for raw data
        /// </summary>
        /// <param name="rawData">Raw data to match</param>
        /// <returns>Best matching rule or null if none found</returns>
        ParsingRule? FindMatchingRule(RawSerialData rawData);

        /// <summary>
        /// Add a new parsing rule
        /// </summary>
        /// <param name="rule">Rule to add</param>
        /// <returns>True if added successfully</returns>
        bool AddRule(ParsingRule rule);

        /// <summary>
        /// Update an existing parsing rule
        /// </summary>
        /// <param name="ruleName">Name of rule to update</param>
        /// <param name="rule">Updated rule</param>
        /// <returns>True if updated successfully</returns>
        bool UpdateRule(string ruleName, ParsingRule rule);

        /// <summary>
        /// Remove a parsing rule
        /// </summary>
        /// <param name="ruleName">Name of rule to remove</param>
        /// <returns>True if removed successfully</returns>
        bool RemoveRule(string ruleName);

        /// <summary>
        /// Validate all parsing rules
        /// </summary>
        /// <returns>Validation result</returns>
        ValidationResult ValidateAllRules();

        /// <summary>
        /// Reload rules from configuration
        /// </summary>
        void ReloadRules();
    }
}