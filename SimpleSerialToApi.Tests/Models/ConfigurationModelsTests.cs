using FluentAssertions;
using SimpleSerialToApi.Models;
using System.IO.Ports;

namespace SimpleSerialToApi.Tests.Models
{
    public class ConfigurationModelsTests
    {
        public class ApiEndpointConfigTests
        {
            [Fact]
            public void Should_InitializeWithDefaultValues()
            {
                // Act
                var config = new ApiEndpointConfig();

                // Assert
                config.Name.Should().Be(string.Empty);
                config.Url.Should().Be(string.Empty);
                config.Method.Should().Be("POST");
                config.AuthType.Should().Be(string.Empty);
                config.AuthToken.Should().Be(string.Empty);
                config.Timeout.Should().Be(30000);
                config.Headers.Should().NotBeNull();
                config.Headers.Should().BeEmpty();
            }

            [Fact]
            public void Should_AllowSettingAllProperties()
            {
                // Act
                var config = new ApiEndpointConfig
                {
                    Name = "TestEndpoint",
                    Url = "https://api.test.com/data",
                    Method = "PUT",
                    AuthType = "Bearer",
                    AuthToken = "test-token",
                    Timeout = 60000,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

                // Assert
                config.Name.Should().Be("TestEndpoint");
                config.Url.Should().Be("https://api.test.com/data");
                config.Method.Should().Be("PUT");
                config.AuthType.Should().Be("Bearer");
                config.AuthToken.Should().Be("test-token");
                config.Timeout.Should().Be(60000);
                config.Headers.Should().ContainKey("Content-Type");
                config.Headers["Content-Type"].Should().Be("application/json");
            }
        }

        public class MappingRuleConfigTests
        {
            [Fact]
            public void Should_InitializeWithDefaultValues()
            {
                // Act
                var config = new MappingRuleConfig();

                // Assert
                config.SourceField.Should().Be(string.Empty);
                config.TargetField.Should().Be(string.Empty);
                config.DataType.Should().Be("string");
                config.Converter.Should().Be(string.Empty);
                config.DefaultValue.Should().Be(string.Empty);
                config.IsRequired.Should().BeFalse();
            }

            [Fact]
            public void Should_AllowSettingAllProperties()
            {
                // Act
                var config = new MappingRuleConfig
                {
                    SourceField = "temperature",
                    TargetField = "temp_celsius",
                    DataType = "decimal",
                    Converter = "TemperatureConverter",
                    DefaultValue = "0.0",
                    IsRequired = true
                };

                // Assert
                config.SourceField.Should().Be("temperature");
                config.TargetField.Should().Be("temp_celsius");
                config.DataType.Should().Be("decimal");
                config.Converter.Should().Be("TemperatureConverter");
                config.DefaultValue.Should().Be("0.0");
                config.IsRequired.Should().BeTrue();
            }
        }

        public class MessageQueueConfigTests
        {
            [Fact]
            public void Should_InitializeWithDefaultValues()
            {
                // Act
                var config = new MessageQueueConfig();

                // Assert
                config.MaxQueueSize.Should().Be(1000);
                config.BatchSize.Should().Be(10);
                config.RetryCount.Should().Be(3);
                config.RetryInterval.Should().Be(5000);
            }

            [Fact]
            public void Should_AllowSettingAllProperties()
            {
                // Act
                var config = new MessageQueueConfig
                {
                    MaxQueueSize = 2000,
                    BatchSize = 20,
                    RetryCount = 5,
                    RetryInterval = 10000
                };

                // Assert
                config.MaxQueueSize.Should().Be(2000);
                config.BatchSize.Should().Be(20);
                config.RetryCount.Should().Be(5);
                config.RetryInterval.Should().Be(10000);
            }

            [Fact]
            public void Should_AcceptZeroValues()
            {
                // Act
                var config = new MessageQueueConfig
                {
                    MaxQueueSize = 0,
                    BatchSize = 0,
                    RetryCount = 0,
                    RetryInterval = 0
                };

                // Assert
                config.MaxQueueSize.Should().Be(0);
                config.BatchSize.Should().Be(0);
                config.RetryCount.Should().Be(0);
                config.RetryInterval.Should().Be(0);
            }
        }

        public class ApplicationConfigTests
        {
            [Fact]
            public void Should_InitializeWithDefaultValues()
            {
                // Act
                var config = new ApplicationConfig();

                // Assert
                config.SerialSettings.Should().NotBeNull();
                config.ApiEndpoints.Should().NotBeNull();
                config.ApiEndpoints.Should().BeEmpty();
                config.MappingRules.Should().NotBeNull();
                config.MappingRules.Should().BeEmpty();
                config.MessageQueueSettings.Should().NotBeNull();
                config.LogLevel.Should().Be("Information");
            }

            [Fact]
            public void Should_InitializeSerialSettingsWithDefaults()
            {
                // Act
                var config = new ApplicationConfig();

                // Assert
                config.SerialSettings.PortName.Should().Be("COM3");
                config.SerialSettings.BaudRate.Should().Be(9600);
                config.SerialSettings.Parity.Should().Be(Parity.None);
                config.SerialSettings.DataBits.Should().Be(8);
                config.SerialSettings.StopBits.Should().Be(StopBits.One);
                config.SerialSettings.Handshake.Should().Be(Handshake.None);
                config.SerialSettings.ReadTimeout.Should().Be(5000);
                config.SerialSettings.WriteTimeout.Should().Be(5000);
            }

            [Fact]
            public void Should_InitializeMessageQueueSettingsWithDefaults()
            {
                // Act
                var config = new ApplicationConfig();

                // Assert
                config.MessageQueueSettings.MaxQueueSize.Should().Be(1000);
                config.MessageQueueSettings.BatchSize.Should().Be(10);
                config.MessageQueueSettings.RetryCount.Should().Be(3);
                config.MessageQueueSettings.RetryInterval.Should().Be(5000);
            }

            [Fact]
            public void Should_AllowSettingAllProperties()
            {
                // Arrange
                var apiEndpoint = new ApiEndpointConfig { Name = "Test", Url = "https://test.com" };
                var mappingRule = new MappingRuleConfig { SourceField = "test", TargetField = "target" };
                var serialSettings = new SerialConnectionSettings { PortName = "COM1", BaudRate = 115200 };
                var queueSettings = new MessageQueueConfig { MaxQueueSize = 500 };

                // Act
                var config = new ApplicationConfig
                {
                    SerialSettings = serialSettings,
                    ApiEndpoints = new List<ApiEndpointConfig> { apiEndpoint },
                    MappingRules = new List<MappingRuleConfig> { mappingRule },
                    MessageQueueSettings = queueSettings,
                    LogLevel = "Debug"
                };

                // Assert
                config.SerialSettings.Should().Be(serialSettings);
                config.ApiEndpoints.Should().HaveCount(1);
                config.ApiEndpoints[0].Should().Be(apiEndpoint);
                config.MappingRules.Should().HaveCount(1);
                config.MappingRules[0].Should().Be(mappingRule);
                config.MessageQueueSettings.Should().Be(queueSettings);
                config.LogLevel.Should().Be("Debug");
            }

            [Fact]
            public void Should_SupportMultipleApiEndpoints()
            {
                // Act
                var config = new ApplicationConfig();
                config.ApiEndpoints.Add(new ApiEndpointConfig { Name = "Endpoint1" });
                config.ApiEndpoints.Add(new ApiEndpointConfig { Name = "Endpoint2" });

                // Assert
                config.ApiEndpoints.Should().HaveCount(2);
                config.ApiEndpoints[0].Name.Should().Be("Endpoint1");
                config.ApiEndpoints[1].Name.Should().Be("Endpoint2");
            }

            [Fact]
            public void Should_SupportMultipleMappingRules()
            {
                // Act
                var config = new ApplicationConfig();
                config.MappingRules.Add(new MappingRuleConfig { SourceField = "field1", TargetField = "target1" });
                config.MappingRules.Add(new MappingRuleConfig { SourceField = "field2", TargetField = "target2" });

                // Assert
                config.MappingRules.Should().HaveCount(2);
                config.MappingRules[0].SourceField.Should().Be("field1");
                config.MappingRules[0].TargetField.Should().Be("target1");
                config.MappingRules[1].SourceField.Should().Be("field2");
                config.MappingRules[1].TargetField.Should().Be("target2");
            }
        }
    }
}