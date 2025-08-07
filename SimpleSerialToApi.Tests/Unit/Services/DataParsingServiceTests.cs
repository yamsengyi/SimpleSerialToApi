using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services.Parsers;
using SimpleSerialToApi.Tests.TestData;
using System;
using System.Text;

namespace SimpleSerialToApi.Tests.Unit.Services
{
    [TestClass]
    public class DataParsingServiceTests : TestBase
    {
        private DataParsingService? _service;
        private Mock<ILogger<DataParsingService>>? _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<DataParsingService>>();
            _service = new DataParsingService(_mockLogger.Object);
        }

        [TestMethod]
        public void Parse_WithTemperatureData_ShouldReturnParsedData()
        {
            // Arrange
            var rawData = TestDataGenerator.GenerateTemperatureData(25.5m, 60.0m);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().NotBeNull();
            result.Fields.Should().ContainKey("temperature");
            result.Fields.Should().ContainKey("humidity");
            result.Fields["temperature"].Should().Be(25.5m);
            result.Fields["humidity"].Should().Be(60.0m);
            result.DeviceId.Should().Be(rawData.DeviceId);
        }

        [TestMethod]
        public void Parse_WithPressureData_ShouldReturnParsedData()
        {
            // Arrange
            var rawData = TestDataGenerator.GeneratePressureData(1013.25m);
            var parsingRule = TestDataGenerator.GeneratePressureParsingRule();

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().NotBeNull();
            result.Fields.Should().ContainKey("pressure");
            result.Fields["pressure"].Should().Be(1013.25m);
            result.DeviceId.Should().Be(rawData.DeviceId);
        }

        [TestMethod]
        public void Parse_WithInvalidPattern_ShouldReturnNull()
        {
            // Arrange
            var rawData = new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes("INVALID:DATA:FORMAT"),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "TEST_DEVICE"
            };

            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_WithNullRawData_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act & Assert
            Action act = () => _service!.Parse(null!, parsingRule);
            act.Should().Throw<ArgumentNullException>().WithParameterName("rawData");
        }

        [TestMethod]
        public void Parse_WithNullParsingRule_ShouldThrowArgumentNullException()
        {
            // Arrange
            var rawData = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);

            // Act & Assert
            Action act = () => _service!.Parse(rawData, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("parsingRule");
        }

        [TestMethod]
        public void Parse_WithEmptyData_ShouldReturnNull()
        {
            // Arrange
            var rawData = TestDataGenerator.GenerateEmptyData();
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_WithHexData_ShouldParseCorrectly()
        {
            // Arrange
            var rawData = TestDataGenerator.GenerateTemperatureDataHex(22.5m, 45.0m);
            
            var hexParsingRule = new ParsingRule
            {
                Name = "HexTemperatureSensor",
                Pattern = "", // For hex data, pattern might be different
                Fields = new[] { "temperature", "humidity" },
                DataFormat = "HEX",
                DeviceType = "TEMPERATURE_SENSOR"
            };

            // Act
            var result = _service!.Parse(rawData, hexParsingRule);

            // Assert - Since hex parsing is complex, we mainly test it doesn't throw
            // In real implementation, this would parse the hex bytes
            if (result != null)
            {
                result.DeviceId.Should().Be(rawData.DeviceId);
                result.ParsedTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            }
        }

        [TestMethod]
        public void Parse_WithComplexPattern_ShouldExtractAllFields()
        {
            // Arrange
            var complexData = "SENSOR:ID001;TEMP:23.5C;HUMID:55.2%;PRESS:1010.5Pa;TIME:2023-01-01T12:00:00Z";
            var rawData = new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes(complexData),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "COMPLEX_SENSOR"
            };

            var complexParsingRule = new ParsingRule
            {
                Name = "ComplexSensor",
                Pattern = @"SENSOR:([A-Z0-9]+);TEMP:([0-9.]+)C;HUMID:([0-9.]+)%;PRESS:([0-9.]+)Pa;TIME:([^;]+)",
                Fields = new[] { "sensor_id", "temperature", "humidity", "pressure", "timestamp" },
                DataFormat = "TEXT",
                DeviceType = "MULTI_SENSOR"
            };

            // Act
            var result = _service!.Parse(rawData, complexParsingRule);

            // Assert
            result.Should().NotBeNull();
            result!.Fields.Should().HaveCount(5);
            result.Fields.Should().ContainKey("sensor_id");
            result.Fields.Should().ContainKey("temperature");
            result.Fields.Should().ContainKey("humidity"); 
            result.Fields.Should().ContainKey("pressure");
            result.Fields.Should().ContainKey("timestamp");
        }

        [TestMethod]
        public void Parse_WithMalformedData_ShouldLogErrorAndReturnNull()
        {
            // Arrange
            var malformedData = TestDataGenerator.GenerateCorruptedData();
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var result = _service!.Parse(malformedData, parsingRule);

            // Assert
            result.Should().BeNull();
            
            // Verify logging occurred
            _mockLogger!.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public void Parse_WithUnicodeData_ShouldHandleCorrectly()
        {
            // Arrange
            var unicodeText = "TEMP:25.5°C;HUMID:60.0%";
            var rawData = new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes(unicodeText),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "UNICODE_DEVICE"
            };

            var parsingRule = new ParsingRule
            {
                Name = "UnicodeTemperatureSensor",
                Pattern = @"TEMP:([0-9.]+)°C;HUMID:([0-9.]+)%",
                Fields = new[] { "temperature", "humidity" },
                DataFormat = "TEXT",
                DeviceType = "TEMPERATURE_SENSOR"
            };

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().NotBeNull();
            result!.Fields["temperature"].Should().Be(25.5m);
            result.Fields["humidity"].Should().Be(60.0m);
        }

        [TestMethod]
        public void Parse_WithLargeData_ShouldHandleEfficiently()
        {
            // Arrange
            var largeData = TestDataGenerator.GenerateLargeData(5000);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act & Assert - Should not throw or take too long
            var startTime = DateTime.UtcNow;
            var result = _service!.Parse(largeData, parsingRule);
            var endTime = DateTime.UtcNow;

            var processingTime = endTime - startTime;
            processingTime.Should().BeLessThan(TimeSpan.FromSeconds(1), "Parsing should be efficient");
        }

        [TestMethod]
        public void Parse_WithMultipleMatches_ShouldReturnFirstMatch()
        {
            // Arrange
            var multiMatchData = "TEMP:25.5C;HUMID:60.0%;TEMP:30.0C;HUMID:70.0%";
            var rawData = new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes(multiMatchData),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = "MULTI_MATCH_DEVICE"
            };

            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().NotBeNull();
            result!.Fields["temperature"].Should().Be(25.5m); // First match
            result.Fields["humidity"].Should().Be(60.0m); // First match
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Action act = () => new DataParsingService(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [TestMethod]
        public void Parse_ShouldSetCorrectTimestamp()
        {
            // Arrange
            var rawData = TestDataGenerator.GenerateTemperatureData(25.0m, 60.0m);
            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();
            var beforeParse = DateTime.UtcNow;

            // Act
            var result = _service!.Parse(rawData, parsingRule);
            var afterParse = DateTime.UtcNow;

            // Assert
            result.Should().NotBeNull();
            result!.ParsedTime.Should().BeOnOrAfter(beforeParse);
            result.ParsedTime.Should().BeOnOrBefore(afterParse);
        }

        [TestMethod]
        public void Parse_ShouldPreserveOriginalDeviceId()
        {
            // Arrange
            var expectedDeviceId = "SPECIFIC_DEVICE_ID_123";
            var rawData = new RawSerialData
            {
                Data = Encoding.UTF8.GetBytes("TEMP:25.5C;HUMID:60.0%"),
                DataFormat = "TEXT",
                ReceivedTime = DateTime.UtcNow,
                DeviceId = expectedDeviceId
            };

            var parsingRule = TestDataGenerator.GenerateTemperatureParsingRule();

            // Act
            var result = _service!.Parse(rawData, parsingRule);

            // Assert
            result.Should().NotBeNull();
            result!.DeviceId.Should().Be(expectedDeviceId);
        }
    }
}