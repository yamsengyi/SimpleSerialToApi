using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Factory for creating and managing data parsers
    /// </summary>
    public class DataParserFactory : IDataParserFactory
    {
        private readonly ILogger<DataParserFactory> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, Type> _registeredParsers;

        public DataParserFactory(ILogger<DataParserFactory> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _registeredParsers = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            RegisterDefaultParsers();
        }

        /// <summary>
        /// Create a parser for the specified data format
        /// </summary>
        /// <param name="dataFormat">Data format (HEX, TEXT, JSON, BINARY)</param>
        /// <returns>Data parser instance or null if format not supported</returns>
        public IDataParser? CreateParser(string dataFormat)
        {
            if (string.IsNullOrWhiteSpace(dataFormat))
            {
                _logger.LogWarning("Data format is null or empty");
                return null;
            }

            if (!_registeredParsers.TryGetValue(dataFormat, out var parserType))
            {
                _logger.LogWarning("No parser registered for data format: {DataFormat}", dataFormat);
                return null;
            }

            try
            {
                // Try to get from service provider first
                if (_serviceProvider.GetService(parserType) is IDataParser serviceParser)
                {
                    return serviceParser;
                }

                // Fallback to Activator.CreateInstance
                var parser = Activator.CreateInstance(parserType);
                if (parser is IDataParser dataParser)
                {
                    _logger.LogDebug("Created parser for format {DataFormat}: {ParserType}", dataFormat, parserType.Name);
                    return dataParser;
                }

                _logger.LogError("Created instance is not an IDataParser: {ParserType}", parserType.Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create parser for format {DataFormat}: {ParserType}", dataFormat, parserType.Name);
                return null;
            }
        }

        /// <summary>
        /// Get a parser that can handle the given raw data
        /// </summary>
        /// <param name="rawData">Raw data to parse</param>
        /// <returns>Suitable parser or null if none found</returns>
        public IDataParser? GetParserForData(RawSerialData rawData)
        {
            if (rawData == null)
            {
                _logger.LogWarning("RawSerialData is null");
                return null;
            }

            // First try to use the specified data format
            if (!string.IsNullOrWhiteSpace(rawData.DataFormat))
            {
                var parser = CreateParser(rawData.DataFormat);
                if (parser?.CanParse(rawData) == true)
                {
                    return parser;
                }
            }

            // Try to find a suitable parser by testing each registered parser
            foreach (var format in _registeredParsers.Keys)
            {
                var parser = CreateParser(format);
                if (parser?.CanParse(rawData) == true)
                {
                    _logger.LogDebug("Found suitable parser {Format} for data", format);
                    return parser;
                }
            }

            _logger.LogWarning("No suitable parser found for data with format {DataFormat}", rawData.DataFormat);
            return null;
        }

        /// <summary>
        /// Register a new parser type
        /// </summary>
        /// <param name="dataFormat">Data format handled by the parser</param>
        /// <param name="parserType">Parser type to register</param>
        public void RegisterParser(string dataFormat, Type parserType)
        {
            if (string.IsNullOrWhiteSpace(dataFormat))
            {
                throw new ArgumentException("Data format cannot be null or empty", nameof(dataFormat));
            }

            if (parserType == null)
            {
                throw new ArgumentNullException(nameof(parserType));
            }

            if (!typeof(IDataParser).IsAssignableFrom(parserType))
            {
                throw new ArgumentException($"Parser type {parserType.Name} does not implement IDataParser", nameof(parserType));
            }

            _registeredParsers.AddOrUpdate(dataFormat, parserType, (key, oldValue) =>
            {
                _logger.LogInformation("Overriding parser for format {DataFormat}: {OldParser} -> {NewParser}", 
                    dataFormat, oldValue.Name, parserType.Name);
                return parserType;
            });

            _logger.LogInformation("Registered parser for format {DataFormat}: {ParserType}", dataFormat, parserType.Name);
        }

        /// <summary>
        /// Get all supported data formats
        /// </summary>
        /// <returns>List of supported formats</returns>
        public string[] GetSupportedFormats()
        {
            return _registeredParsers.Keys.ToArray();
        }

        /// <summary>
        /// Register the default parsers
        /// </summary>
        private void RegisterDefaultParsers()
        {
            try
            {
                RegisterParser("TEXT", typeof(TextDataParser));
                RegisterParser("HEX", typeof(HexDataParser));
                RegisterParser("JSON", typeof(JsonDataParser));
                RegisterParser("BINARY", typeof(BinaryDataParser));

                _logger.LogInformation("Default parsers registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register default parsers");
            }
        }
    }
}