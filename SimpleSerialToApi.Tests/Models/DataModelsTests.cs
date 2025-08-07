using FluentAssertions;
using SimpleSerialToApi.Models;
using System;
using Xunit;

namespace SimpleSerialToApi.Tests.Models
{
    public class DataModelsTests
    {
        public class RawSerialDataTests
        {
            [Fact]
            public void Should_CreateWithDefaultValues()
            {
                // Act
                var rawData = new RawSerialData();

                // Assert
                rawData.ReceivedTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
                rawData.Data.Should().BeEmpty();
                rawData.DataFormat.Should().Be(string.Empty);
                rawData.DeviceId.Should().Be(string.Empty);
                rawData.PortName.Should().Be(string.Empty);
            }

            [Fact]
            public void Should_CreateWithSpecifiedValues()
            {
                // Arrange
                var testData = System.Text.Encoding.UTF8.GetBytes("TEMP:25.5C");
                var dataFormat = "TEXT";
                var deviceId = "SENSOR01";
                var portName = "COM3";

                // Act
                var rawData = new RawSerialData(testData, dataFormat, deviceId, portName);

                // Assert
                rawData.Data.Should().Equal(testData);
                rawData.DataFormat.Should().Be(dataFormat);
                rawData.DeviceId.Should().Be(deviceId);
                rawData.PortName.Should().Be(portName);
                rawData.ReceivedTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            }

            [Fact]
            public void Should_HandleNullData()
            {
                // Act
                var rawData = new RawSerialData(null, "TEXT");

                // Assert
                rawData.Data.Should().BeEmpty();
            }
        }

        public class ParsedDataTests
        {
            [Fact]
            public void Should_CreateWithDefaultValues()
            {
                // Act
                var parsedData = new ParsedData();

                // Assert
                parsedData.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
                parsedData.DeviceId.Should().Be(string.Empty);
                parsedData.Fields.Should().BeEmpty();
                parsedData.DataSource.Should().Be(string.Empty);
                parsedData.OriginalData.Should().BeNull();
                parsedData.AppliedRule.Should().BeNull();
            }

            [Fact]
            public void Should_CreateWithSpecifiedValues()
            {
                // Arrange
                var deviceId = "SENSOR01";
                var dataSource = "COM3";

                // Act
                var parsedData = new ParsedData(deviceId, dataSource);

                // Assert
                parsedData.DeviceId.Should().Be(deviceId);
                parsedData.DataSource.Should().Be(dataSource);
                parsedData.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            }

            [Fact]
            public void Should_AllowFieldManipulation()
            {
                // Arrange
                var parsedData = new ParsedData();

                // Act
                parsedData.Fields["temperature"] = 25.5m;
                parsedData.Fields["humidity"] = 60.2m;

                // Assert
                parsedData.Fields.Should().HaveCount(2);
                parsedData.Fields["temperature"].Should().Be(25.5m);
                parsedData.Fields["humidity"].Should().Be(60.2m);
            }
        }

        public class MappedApiDataTests
        {
            [Fact]
            public void Should_CreateWithDefaultValues()
            {
                // Act
                var mappedData = new MappedApiData();

                // Assert
                mappedData.EndpointName.Should().Be(string.Empty);
                mappedData.Payload.Should().BeEmpty();
                mappedData.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
                mappedData.MessageId.Should().NotBeNullOrEmpty();
                mappedData.OriginalParsedData.Should().BeNull();
                mappedData.Priority.Should().Be(5);
                mappedData.RetryCount.Should().Be(0);
                mappedData.MaxRetries.Should().Be(3);
            }

            [Fact]
            public void Should_CreateWithSpecifiedValues()
            {
                // Arrange
                var endpointName = "SensorDataEndpoint";
                var parsedData = new ParsedData("SENSOR01", "COM3");

                // Act
                var mappedData = new MappedApiData(endpointName, parsedData);

                // Assert
                mappedData.EndpointName.Should().Be(endpointName);
                mappedData.OriginalParsedData.Should().Be(parsedData);
                mappedData.MessageId.Should().NotBeNullOrEmpty();
            }

            [Fact]
            public void Should_GenerateUniqueMessageIds()
            {
                // Act
                var mappedData1 = new MappedApiData();
                var mappedData2 = new MappedApiData();

                // Assert
                mappedData1.MessageId.Should().NotBe(mappedData2.MessageId);
            }
        }

        public class ParsingResultTests
        {
            [Fact]
            public void Should_CreateSuccessResult()
            {
                // Arrange
                var parsedData = new ParsedData("SENSOR01");
                var duration = TimeSpan.FromMilliseconds(50);

                // Act
                var result = ParsingResult.Success(parsedData, duration);

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.ParsedData.Should().Be(parsedData);
                result.ErrorMessage.Should().BeNull();
                result.Exception.Should().BeNull();
                result.ParseDuration.Should().Be(duration);
            }

            [Fact]
            public void Should_CreateFailureResult()
            {
                // Arrange
                var errorMessage = "Parse failed";
                var exception = new InvalidOperationException("Test exception");
                var duration = TimeSpan.FromMilliseconds(25);

                // Act
                var result = ParsingResult.Failure(errorMessage, exception, duration);

                // Assert
                result.IsSuccess.Should().BeFalse();
                result.ParsedData.Should().BeNull();
                result.ErrorMessage.Should().Be(errorMessage);
                result.Exception.Should().Be(exception);
                result.ParseDuration.Should().Be(duration);
            }
        }

        public class MappingResultTests
        {
            [Fact]
            public void Should_CreateSuccessResult()
            {
                // Arrange
                var mappedData = new MappedApiData("TestEndpoint");
                var duration = TimeSpan.FromMilliseconds(100);

                // Act
                var result = MappingResult.Success(mappedData, duration);

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.MappedData.Should().Be(mappedData);
                result.ErrorMessage.Should().BeNull();
                result.Exception.Should().BeNull();
                result.MappingDuration.Should().Be(duration);
            }

            [Fact]
            public void Should_CreateFailureResult()
            {
                // Arrange
                var errorMessage = "Mapping failed";
                var exception = new InvalidOperationException("Test exception");
                var duration = TimeSpan.FromMilliseconds(75);

                // Act
                var result = MappingResult.Failure(errorMessage, exception, duration);

                // Assert
                result.IsSuccess.Should().BeFalse();
                result.MappedData.Should().BeNull();
                result.ErrorMessage.Should().Be(errorMessage);
                result.Exception.Should().Be(exception);
                result.MappingDuration.Should().Be(duration);
            }
        }

        public class ValidationResultTests
        {
            [Fact]
            public void Should_CreateValidResult()
            {
                // Act
                var result = ValidationResult.Valid();

                // Assert
                result.IsValid.Should().BeTrue();
                result.Errors.Should().BeEmpty();
                result.Warnings.Should().BeEmpty();
            }

            [Fact]
            public void Should_CreateInvalidResult()
            {
                // Arrange
                var errors = new[] { "Error 1", "Error 2" };

                // Act
                var result = ValidationResult.Invalid(errors);

                // Assert
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Equal(errors);
                result.Warnings.Should().BeEmpty();
            }

            [Fact]
            public void Should_AddErrorsAndWarnings()
            {
                // Arrange
                var result = new ValidationResult { IsValid = true };

                // Act
                result.AddError("Test error");
                result.AddWarning("Test warning");

                // Assert
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("Test error");
                result.Warnings.Should().Contain("Test warning");
            }
        }
    }
}