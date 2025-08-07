using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SimpleSerialToApi.Tests.Services.Parsers
{
    public class TextDataParserTests
    {
        private readonly Mock<ILogger<TextDataParser>> _loggerMock;
        private readonly TextDataParser _parser;

        public TextDataParserTests()
        {
            _loggerMock = new Mock<ILogger<TextDataParser>>();
            _parser = new TextDataParser(_loggerMock.Object);
        }

        public class SupportedFormat : TextDataParserTests
        {
            [Fact]
            public void Should_ReturnTextFormat()
            {
                // Act
                var format = _parser.SupportedFormat;

                // Assert
                format.Should().Be("TEXT");
            }
        }

        public class CanParse : TextDataParserTests
        {
            [Fact]
            public void Should_ReturnTrue_WhenDataIsValidText()
            {
                // Arrange
                var textData = "TEMP:25.5C;HUMID:60.2%";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                // Act
                var result = _parser.CanParse(rawData);

                // Assert
                result.Should().BeTrue();
            }

            [Fact]
            public void Should_ReturnTrue_WhenFormatIsEmpty()
            {
                // Arrange
                var textData = "TEMP:25.5C;HUMID:60.2%";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "");

                // Act
                var result = _parser.CanParse(rawData);

                // Assert
                result.Should().BeTrue();
            }

            [Fact]
            public void Should_ReturnFalse_WhenFormatDoesNotMatch()
            {
                // Arrange
                var textData = "TEMP:25.5C;HUMID:60.2%";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "HEX");

                // Act
                var result = _parser.CanParse(rawData);

                // Assert
                result.Should().BeFalse();
            }

            [Fact]
            public void Should_ReturnFalse_WhenDataIsNull()
            {
                // Arrange
                var rawData = new RawSerialData(null, "TEXT");

                // Act
                var result = _parser.CanParse(rawData);

                // Assert
                result.Should().BeFalse();
            }

            [Fact]
            public void Should_ReturnFalse_WhenDataIsEmpty()
            {
                // Arrange
                var rawData = new RawSerialData(Array.Empty<byte>(), "TEXT");

                // Act
                var result = _parser.CanParse(rawData);

                // Assert
                result.Should().BeFalse();
            }

            [Fact]
            public void Should_ReturnFalse_WhenDataContainsManyNullCharacters()
            {
                // Arrange
                var binaryData = new byte[] { 0x00, 0x00, 0x00, 0x41, 0x00, 0x00 }; // Many nulls with 'A'
                var rawData = new RawSerialData(binaryData, "TEXT");

                // Act
                var result = _parser.CanParse(rawData);

                // Assert
                result.Should().BeFalse();
            }
        }

        public class Parse : TextDataParserTests
        {
            [Fact]
            public void Should_ParseTemperatureAndHumidity_Successfully()
            {
                // Arrange
                var textData = "TEMP:25.5C;HUMID:60.2%";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT", "SENSOR01", "COM3");

                var rule = new ParsingRule
                {
                    Name = "TempHumidRule",
                    Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%",
                    Fields = new List<string> { "temperature", "humidity" },
                    DataTypes = new List<string> { "decimal", "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.ParsedData.Should().NotBeNull();
                result.ParsedData!.DeviceId.Should().Be("SENSOR01");
                result.ParsedData.DataSource.Should().Be("COM3");
                result.ParsedData.AppliedRule.Should().Be("TempHumidRule");
                result.ParsedData.Fields.Should().HaveCount(2);
                result.ParsedData.Fields["temperature"].Should().Be(25.5m);
                result.ParsedData.Fields["humidity"].Should().Be(60.2m);
                result.ParseDuration.Should().BeGreaterThan(TimeSpan.Zero);
            }

            [Fact]
            public void Should_ParseIntegerValues_Successfully()
            {
                // Arrange
                var textData = "COUNT:42";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                var rule = new ParsingRule
                {
                    Name = "CountRule",
                    Pattern = @"COUNT:([0-9]+)",
                    Fields = new List<string> { "count" },
                    DataTypes = new List<string> { "int" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.ParsedData!.Fields["count"].Should().Be(42);
            }

            [Fact]
            public void Should_ParseBooleanValues_Successfully()
            {
                // Arrange
                var textData = "ACTIVE:true;ENABLED:false";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                var rule = new ParsingRule
                {
                    Name = "BoolRule",
                    Pattern = @"ACTIVE:([a-z]+);ENABLED:([a-z]+)",
                    Fields = new List<string> { "active", "enabled" },
                    DataTypes = new List<string> { "bool", "bool" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.ParsedData!.Fields["active"].Should().Be(true);
                result.ParsedData.Fields["enabled"].Should().Be(false);
            }

            [Fact]
            public void Should_ReturnFailure_WhenRawDataIsNull()
            {
                // Arrange
                var rule = new ParsingRule { Name = "TestRule" };

                // Act
                var result = _parser.Parse(null, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().Be("Raw data is null or empty");
            }

            [Fact]
            public void Should_ReturnFailure_WhenRuleIsNull()
            {
                // Arrange
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes("test"), "TEXT");

                // Act
                var result = _parser.Parse(rawData, null);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().Be("Parsing rule is null");
            }

            [Fact]
            public void Should_ReturnFailure_WhenPatternDoesNotMatch()
            {
                // Arrange
                var textData = "TEMP:25.5C";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                var rule = new ParsingRule
                {
                    Name = "MismatchRule",
                    Pattern = @"PRESSURE:([0-9.]+)hPa", // Different pattern
                    Fields = new List<string> { "pressure" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().Contain("Pattern did not match data");
            }

            [Fact]
            public void Should_ReturnFailure_WhenRegexPatternIsInvalid()
            {
                // Arrange
                var textData = "TEMP:25.5C";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                var rule = new ParsingRule
                {
                    Name = "InvalidRegexRule",
                    Pattern = @"[", // Invalid regex
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().StartWith("Parse error:");
                result.ErrorMessage.Should().Contain("Invalid pattern");
            }

            [Fact]
            public void Should_UseDefaultValue_WhenConversionFails()
            {
                // Arrange
                var textData = "TEMP:invalidnumber";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                var rule = new ParsingRule
                {
                    Name = "ConversionFailRule",
                    Pattern = @"TEMP:([a-z]+)", // Captures letters
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" }, // But expects decimal
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.ParsedData!.Fields["temperature"].Should().Be(0m); // Default decimal value
            }

            [Fact]
            public void Should_HandleMissingRegexGroups_Gracefully()
            {
                // Arrange
                var textData = "TEMP:25.5C";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");

                var rule = new ParsingRule
                {
                    Name = "MissingGroupsRule",
                    Pattern = @"TEMP:([0-9.]+)C", // Only one capture group
                    Fields = new List<string> { "temperature", "humidity" }, // But two fields expected
                    DataTypes = new List<string> { "decimal", "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.Parse(rawData, rule);

                // Assert
                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.ParsedData!.Fields["temperature"].Should().Be(25.5m);
                result.ParsedData.Fields["humidity"].Should().Be(0m); // Default value for missing group
            }
        }

        public class ValidateRule : TextDataParserTests
        {
            [Fact]
            public void Should_ReturnValid_WhenRuleIsCorrect()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "ValidRule",
                    Pattern = @"TEMP:([0-9.]+)C",
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeTrue();
                result.Errors.Should().BeEmpty();
            }

            [Fact]
            public void Should_ReturnInvalid_WhenRuleIsNull()
            {
                // Act
                var result = _parser.ValidateRule(null);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("Parsing rule is null");
            }

            [Fact]
            public void Should_ReturnInvalid_WhenDataFormatDoesNotMatch()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "InvalidFormatRule",
                    Pattern = @"TEMP:([0-9.]+)C",
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "JSON" // Wrong format for TEXT parser
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("Data format 'JSON' is not supported by TEXT parser");
            }

            [Fact]
            public void Should_ReturnInvalid_WhenPatternIsEmpty()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "EmptyPatternRule",
                    Pattern = "",
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("Pattern is required");
            }

            [Fact]
            public void Should_ReturnInvalid_WhenNoFieldsDefined()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "NoFieldsRule",
                    Pattern = @"TEMP:([0-9.]+)C",
                    Fields = new List<string>(), // Empty
                    DataTypes = new List<string>(),
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("At least one field must be defined");
            }

            [Fact]
            public void Should_ReturnInvalid_WhenFieldCountDoesNotMatchDataTypeCount()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "MismatchCountRule",
                    Pattern = @"TEMP:([0-9.]+)C",
                    Fields = new List<string> { "temperature", "humidity" }, // 2 fields
                    DataTypes = new List<string> { "decimal" }, // 1 data type
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("Field count (2) does not match data type count (1)");
            }

            [Fact]
            public void Should_ReturnInvalid_WhenDataTypeIsInvalid()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "InvalidDataTypeRule",
                    Pattern = @"TEMP:([0-9.]+)C",
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "invalidtype" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("Invalid data type: invalidtype");
            }

            [Fact]
            public void Should_ReturnInvalid_WhenRegexPatternIsInvalid()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "InvalidRegexRule",
                    Pattern = @"[", // Invalid regex
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().HaveCount(1);
                result.Errors[0].Should().StartWith("Invalid regex pattern:");
            }

            [Fact]
            public void Should_ReturnWarning_WhenCaptureGroupCountDoesNotMatchFieldCount()
            {
                // Arrange
                var rule = new ParsingRule
                {
                    Name = "GroupCountMismatchRule",
                    Pattern = @"TEMP:([0-9.]+)C;HUMID:([0-9.]+)%", // 2 capture groups
                    Fields = new List<string> { "temperature" }, // 1 field
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                var result = _parser.ValidateRule(rule);

                // Assert
                result.Should().NotBeNull();
                result.Warnings.Should().Contain("Regex pattern has 2 capture groups but 1 fields are defined");
            }
        }

        public class GetPerformanceMetrics : TextDataParserTests
        {
            [Fact]
            public void Should_ReturnPerformanceMetrics()
            {
                // Act
                var metrics = _parser.GetPerformanceMetrics();

                // Assert
                metrics.Should().NotBeNull();
                metrics.Should().ContainKey("ParseCount");
                metrics.Should().ContainKey("ParseErrorCount");
                metrics.Should().ContainKey("LastParseTime");
                metrics.Should().ContainKey("AverageParseTime");
                metrics.Should().ContainKey("RegexCacheSize");
            }

            [Fact]
            public void Should_UpdateMetrics_AfterSuccessfulParse()
            {
                // Arrange
                var textData = "TEMP:25.5C";
                var rawData = new RawSerialData(Encoding.UTF8.GetBytes(textData), "TEXT");
                var rule = new ParsingRule
                {
                    Name = "TestRule",
                    Pattern = @"TEMP:([0-9.]+)C",
                    Fields = new List<string> { "temperature" },
                    DataTypes = new List<string> { "decimal" },
                    DataFormat = "TEXT"
                };

                // Act
                _parser.Parse(rawData, rule);
                var metrics = _parser.GetPerformanceMetrics();

                // Assert
                metrics["ParseCount"].Should().Be(1L);
                metrics["ParseErrorCount"].Should().Be(0L);
                ((double)metrics["LastParseTime"]).Should().BeGreaterThan(0);
            }
        }
    }
}