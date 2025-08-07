using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SimpleSerialToApi.Tests.Integration
{
    public class DataParsingIntegrationTests
    {
        private readonly Mock<ILogger<DataParserFactory>> _factoryLoggerMock;
        private readonly Mock<ILogger<TextDataParser>> _parserLoggerMock;
        private readonly Mock<ILogger<DataMappingEngine>> _mappingLoggerMock;
        private readonly Mock<IConfigurationService> _configServiceMock;
        private readonly DataParserFactory _parserFactory;
        private readonly DataMappingEngine _mappingEngine;

        public DataParsingIntegrationTests()
        {
            _factoryLoggerMock = new Mock<ILogger<DataParserFactory>>();
            _parserLoggerMock = new Mock<ILogger<TextDataParser>>();
            _mappingLoggerMock = new Mock<ILogger<DataMappingEngine>>();
            _configServiceMock = new Mock<IConfigurationService>();

            // Setup mock service provider
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(TextDataParser)))
                .Returns(new TextDataParser(_parserLoggerMock.Object));
            serviceProviderMock.Setup(sp => sp.GetService(typeof(HexDataParser)))
                .Returns(new HexDataParser());
            serviceProviderMock.Setup(sp => sp.GetService(typeof(JsonDataParser)))
                .Returns(new JsonDataParser());
            serviceProviderMock.Setup(sp => sp.GetService(typeof(BinaryDataParser)))
                .Returns(new BinaryDataParser());

            _parserFactory = new DataParserFactory(_factoryLoggerMock.Object, serviceProviderMock.Object);

            // Setup configuration service mock
            var apiEndpoints = new List<ApiEndpointConfig>
            {
                new ApiEndpointConfig { Name = "SensorDataEndpoint", Url = "https://api.example.com/sensor-data" }
            };

            var mappingRules = new List<MappingRuleConfig>
            {
                new MappingRuleConfig 
                { 
                    SourceField = "temperature", 
                    TargetField = "temp_celsius", 
                    DataType = "decimal",
                    IsRequired = true 
                },
                new MappingRuleConfig 
                { 
                    SourceField = "humidity", 
                    TargetField = "humidity_percent", 
                    DataType = "decimal",
                    IsRequired = false 
                }
            };

            _configServiceMock.Setup(cs => cs.ApiEndpoints).Returns(apiEndpoints);
            _configServiceMock.Setup(cs => cs.MappingRules).Returns(mappingRules);

            _mappingEngine = new DataMappingEngine(_mappingLoggerMock.Object, _configServiceMock.Object);
        }

        [Fact]
        public async Task Should_ParseAndMapTemperatureHumidityData_EndToEnd()
        {
            // Arrange
            var textData = "TEMP:25.5C;HUMID:60.2%";
            var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT", "SENSOR01", "COM3");

            var rule = new ParsingRule
            {
                Name = "TempHumidityRule",
                Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%",
                Fields = new List<string> { "temperature", "humidity" },
                DataTypes = new List<string> { "decimal", "decimal" },
                DataFormat = "TEXT"
            };

            // Act - Parse
            var parser = _parserFactory.CreateParser("TEXT");
            parser.Should().NotBeNull();

            var parseResult = parser!.Parse(rawData, rule);
            parseResult.IsSuccess.Should().BeTrue();

            // Act - Map
            var mapResult = _mappingEngine.MapToApiData(parseResult.ParsedData!, "SensorDataEndpoint");

            // Assert
            mapResult.Should().NotBeNull();
            mapResult.IsSuccess.Should().BeTrue();
            
            var mappedData = mapResult.MappedData!;
            mappedData.EndpointName.Should().Be("SensorDataEndpoint");
            mappedData.Payload.Should().ContainKey("temp_celsius");
            mappedData.Payload.Should().ContainKey("humidity_percent");
            mappedData.Payload.Should().ContainKey("timestamp");
            mappedData.Payload.Should().ContainKey("deviceId");
            
            mappedData.Payload["temp_celsius"].Should().Be(25.5m);
            mappedData.Payload["humidity_percent"].Should().Be(60.2m);
            mappedData.Payload["deviceId"].Should().Be("SENSOR01");
        }

        [Fact]
        public void Should_HandleParsingFailure_Gracefully()
        {
            // Arrange
            var textData = "INVALID DATA FORMAT";
            var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

            var rule = new ParsingRule
            {
                Name = "TempHumidityRule",
                Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%",
                Fields = new List<string> { "temperature", "humidity" },
                DataTypes = new List<string> { "decimal", "decimal" },
                DataFormat = "TEXT"
            };

            // Act
            var parser = _parserFactory.CreateParser("TEXT");
            var parseResult = parser!.Parse(rawData, rule);

            // Assert
            parseResult.Should().NotBeNull();
            parseResult.IsSuccess.Should().BeFalse();
            parseResult.ErrorMessage.Should().Contain("Pattern did not match data");
            parseResult.ParsedData.Should().BeNull();
        }

        [Fact]
        public async Task Should_ProcessBatchData_WithinPerformanceTarget()
        {
            // Arrange
            var batchSize = 100;
            var parsedDataList = new List<ParsedData>();

            for (int i = 0; i < batchSize; i++)
            {
                var parsedData = new ParsedData($"SENSOR{i:D3}", "COM3");
                parsedData.Fields["temperature"] = 20.0m + i * 0.1m;
                parsedData.Fields["humidity"] = 50.0m + i * 0.2m;
                parsedDataList.Add(parsedData);
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            var results = await _mappingEngine.MapBatchAsync(parsedDataList);
            stopwatch.Stop();

            // Assert
            results.Should().HaveCount(batchSize);
            results.Should().OnlyContain(r => r.IsSuccess);
            
            // Performance target: < 500ms for 100 items
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "batch mapping should complete within performance target");

            // Verify some results
            results[0].MappedData!.Payload["temp_celsius"].Should().Be(20.0m);
            results[50].MappedData!.Payload["temp_celsius"].Should().Be(25.0m);
            results[99].MappedData!.Payload["temp_celsius"].Should().Be(29.9m);
        }

        [Fact]
        public void Should_MeetSingleMessageParsingPerformanceTarget()
        {
            // Arrange
            var textData = "TEMP:25.5C;HUMID:60.2%";
            var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT", "SENSOR01");

            var rule = new ParsingRule
            {
                Name = "TempHumidityRule",
                Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%",
                Fields = new List<string> { "temperature", "humidity" },
                DataTypes = new List<string> { "decimal", "decimal" },
                DataFormat = "TEXT"
            };

            var parser = _parserFactory.CreateParser("TEXT");

            // Act & Assert - Warm up
            parser!.Parse(rawData, rule);

            // Act & Assert - Actual measurement
            var stopwatch = Stopwatch.StartNew();
            var result = parser.Parse(rawData, rule);
            stopwatch.Stop();

            // Assert
            result.IsSuccess.Should().BeTrue();
            
            // Performance target: < 50ms for single message parsing
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "single message parsing should complete within performance target");
            result.ParseDuration.TotalMilliseconds.Should().BeLessThan(50, "parser internal timing should also meet target");
        }

        [Fact]
        public void Should_HandleMultipleParsersCorrectly()
        {
            // Arrange & Act
            var textParser = _parserFactory.CreateParser("TEXT");
            var hexParser = _parserFactory.CreateParser("HEX");
            var jsonParser = _parserFactory.CreateParser("JSON");
            var binaryParser = _parserFactory.CreateParser("BINARY");

            // Assert
            textParser.Should().NotBeNull();
            textParser!.SupportedFormat.Should().Be("TEXT");

            hexParser.Should().NotBeNull();
            hexParser!.SupportedFormat.Should().Be("HEX");

            jsonParser.Should().NotBeNull();
            jsonParser!.SupportedFormat.Should().Be("JSON");

            binaryParser.Should().NotBeNull();
            binaryParser!.SupportedFormat.Should().Be("BINARY");
        }

        [Fact]
        public void Should_ReturnNullForUnsupportedFormat()
        {
            // Act
            var unsupportedParser = _parserFactory.CreateParser("UNSUPPORTED");

            // Assert
            unsupportedParser.Should().BeNull();
        }

        [Fact]
        public void Should_TrackPerformanceMetrics()
        {
            // Arrange
            var parser = _parserFactory.CreateParser("TEXT");
            var textData = "TEMP:25.5C";
            var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");
            var rule = new ParsingRule
            {
                Name = "TempRule",
                Pattern = @"TEMP:([0-9.]+)C",
                Fields = new List<string> { "temperature" },
                DataTypes = new List<string> { "decimal" },
                DataFormat = "TEXT"
            };

            // Act
            parser!.Parse(rawData, rule);
            parser.Parse(rawData, rule);
            var metrics = parser.GetPerformanceMetrics();

            // Assert
            metrics.Should().ContainKey("ParseCount");
            metrics.Should().ContainKey("ParseErrorCount");
            metrics.Should().ContainKey("LastParseTime");
            metrics.Should().ContainKey("AverageParseTime");

            metrics["ParseCount"].Should().Be(2L);
            metrics["ParseErrorCount"].Should().Be(0L);
            ((double)metrics["LastParseTime"]).Should().BeGreaterThan(0);
            ((double)metrics["AverageParseTime"]).Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("JSON", @"{""temperature"":25.5,""humidity"":60.2}")]
        [InlineData("HEX", "19FF3C14")] // Example hex data
        [InlineData("BINARY", "Data will be processed as binary")]
        public void Should_IdentifyCorrectParserForDataFormat(string format, string testData)
        {
            // Arrange
            var data = format == "BINARY" ? new byte[] { 0x25, 0x60, 0x19, 0xFF } : Encoding.UTF8.GetBytes(testData);
            var rawData = new RawSerialData(data, format);

            // Act
            var parser = _parserFactory.GetParserForData(rawData);

            // Assert
            parser.Should().NotBeNull();
            parser!.SupportedFormat.Should().Be(format);
            parser.CanParse(rawData).Should().BeTrue();
        }
    }
}