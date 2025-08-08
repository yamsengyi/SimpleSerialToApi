using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Configuration;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using SimpleSerialToApi.Services;
using FluentAssertions;
using System.Configuration;

namespace SimpleSerialToApi.Tests.Services
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly ConfigurationService _configurationService;

        public ConfigurationServiceTests()
        {
            _logger = new LoggerFactory().CreateLogger<ConfigurationService>();
            _configurationService = new ConfigurationService(_logger);
        }

        public void Dispose()
        {
            _configurationService?.Dispose();
        }

        public class Constructor : ConfigurationServiceTests
        {
            [Fact]
            public void Should_InitializeSuccessfully()
            {
                // Act & Assert
                _configurationService.Should().NotBeNull();
                _configurationService.ApplicationConfig.Should().NotBeNull();
            }

            [Fact]
            public void Should_LoadDefaultConfiguration()
            {
                // Act
                var config = _configurationService.ApplicationConfig;

                // Assert
                config.Should().NotBeNull();
                config.SerialSettings.Should().NotBeNull();
                config.MessageQueueSettings.Should().NotBeNull();
                config.ApiEndpoints.Should().NotBeNull();
                config.MappingRules.Should().NotBeNull();
            }
        }

        public class GetAppSetting : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnSerialPortSetting()
            {
                // Act
                var serialPort = _configurationService.GetAppSetting("SerialPort");

                // Assert
                serialPort.Should().NotBeNullOrEmpty();
            }

            [Fact]
            public void Should_ReturnBaudRateSetting()
            {
                // Act
                var baudRate = _configurationService.GetAppSetting("BaudRate");

                // Assert
                baudRate.Should().NotBeNullOrEmpty();
            }

            [Fact]
            public void Should_ReturnEmptyString_WhenKeyNotFound()
            {
                // Act
                var result = _configurationService.GetAppSetting("NonExistentKey");

                // Assert
                result.Should().Be(string.Empty);
            }

            [Fact]
            public void Should_ReturnLogLevelSetting()
            {
                // Act
                var logLevel = _configurationService.GetAppSetting("LogLevel");

                // Assert
                logLevel.Should().NotBeNullOrEmpty();
            }
        }

        public class ApplicationConfigProperty : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnValidApplicationConfig()
            {
                // Act
                var config = _configurationService.ApplicationConfig;

                // Assert
                config.Should().NotBeNull();
                config.SerialSettings.Should().NotBeNull();
                config.SerialSettings.PortName.Should().NotBeNullOrEmpty();
                config.SerialSettings.BaudRate.Should().BeGreaterThan(0);
            }

            [Fact]
            public void Should_LoadSerialSettings()
            {
                // Act
                var serialSettings = _configurationService.ApplicationConfig.SerialSettings;

                // Assert
                serialSettings.Should().NotBeNull();
                serialSettings.PortName.Should().NotBeNullOrEmpty();
                serialSettings.BaudRate.Should().BeGreaterThan(0);
                serialSettings.DataBits.Should().BeGreaterThan(0);
                serialSettings.ReadTimeout.Should().BeGreaterThan(0);
                serialSettings.WriteTimeout.Should().BeGreaterThan(0);
            }

            [Fact]
            public void Should_LoadMessageQueueSettings()
            {
                // Act
                var queueSettings = _configurationService.ApplicationConfig.MessageQueueSettings;

                // Assert
                queueSettings.Should().NotBeNull();
                queueSettings.MaxQueueSize.Should().BeGreaterThan(0);
                queueSettings.BatchSize.Should().BeGreaterThan(0);
                queueSettings.RetryCount.Should().BeGreaterThanOrEqualTo(0);
                queueSettings.RetryInterval.Should().BeGreaterThan(0);
            }
        }

        public class ApiEndpointsProperty : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnApiEndpoints()
            {
                // Act
                var endpoints = _configurationService.ApiEndpoints;

                // Assert
                endpoints.Should().NotBeNull();
                // Note: May be empty if not configured in test environment
            }

            [Fact]
            public void Should_LoadApiEndpointsFromConfiguration()
            {
                // Act
                var endpoints = _configurationService.ApiEndpoints.ToList();

                // Assert
                endpoints.Should().NotBeNull();
                
                // If endpoints are configured, validate their structure
                foreach (var endpoint in endpoints)
                {
                    endpoint.Name.Should().NotBeNullOrEmpty();
                    endpoint.Url.Should().NotBeNullOrEmpty();
                    endpoint.Method.Should().NotBeNullOrEmpty();
                    endpoint.Timeout.Should().BeGreaterThan(0);
                }
            }
        }

        public class MappingRulesProperty : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnMappingRules()
            {
                // Act
                var rules = _configurationService.MappingRules;

                // Assert
                rules.Should().NotBeNull();
                // Note: May be empty if not configured in test environment
            }

            [Fact]
            public void Should_LoadMappingRulesFromConfiguration()
            {
                // Act
                var rules = _configurationService.MappingRules.ToList();

                // Assert
                rules.Should().NotBeNull();
                
                // If rules are configured, validate their structure
                foreach (var rule in rules)
                {
                    rule.SourceField.Should().NotBeNullOrEmpty();
                    rule.TargetField.Should().NotBeNullOrEmpty();
                    rule.DataType.Should().NotBeNullOrEmpty();
                }
            }
        }

        public class ValidateConfiguration : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnTrue_WhenConfigurationIsValid()
            {
                // Act
                var isValid = _configurationService.ValidateConfiguration();

                // Assert
                isValid.Should().BeTrue();
            }

            [Fact]
            public void Should_ValidateSerialSettings()
            {
                // Act
                var isValid = _configurationService.ValidateConfiguration();

                // Assert
                var config = _configurationService.ApplicationConfig;
                config.SerialSettings.PortName.Should().NotBeNullOrEmpty();
                config.SerialSettings.BaudRate.Should().BeGreaterThan(0);
                
                // If validation passes, these should all be valid
                if (isValid)
                {
                    config.SerialSettings.PortName.Should().NotBeNullOrEmpty();
                    config.SerialSettings.BaudRate.Should().BeGreaterThan(0);
                }
            }

            [Fact]
            public void Should_ValidateMessageQueueSettings()
            {
                // Act
                var isValid = _configurationService.ValidateConfiguration();

                // Assert
                var config = _configurationService.ApplicationConfig;
                
                // If validation passes, queue settings should be valid
                if (isValid)
                {
                    config.MessageQueueSettings.MaxQueueSize.Should().BeGreaterThan(0);
                    config.MessageQueueSettings.BatchSize.Should().BeGreaterThan(0);
                    config.MessageQueueSettings.RetryCount.Should().BeGreaterThanOrEqualTo(0);
                    config.MessageQueueSettings.RetryInterval.Should().BeGreaterThan(0);
                }
            }
        }

        public class ReloadConfiguration : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReloadConfigurationSuccessfully()
            {
                // Arrange
                var configurationChangedFired = false;
                _configurationService.ConfigurationChanged += (sender, args) =>
                {
                    configurationChangedFired = true;
                };

                // Act
                _configurationService.ReloadConfiguration();

                // Assert
                configurationChangedFired.Should().BeTrue();
            }

            [Fact]
            public void Should_RefreshAppSettings_WhenReloadCalled()
            {
                // Arrange
                var beforeReload = _configurationService.GetAppSetting("SerialPort");

                // Act
                _configurationService.ReloadConfiguration();
                var afterReload = _configurationService.GetAppSetting("SerialPort");

                // Assert
                beforeReload.Should().NotBeNullOrEmpty();
                afterReload.Should().NotBeNullOrEmpty();
                afterReload.Should().Be(beforeReload); // Should be the same unless config changed
            }
        }

        public class GetSection : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnNewInstance_WhenSectionNotFound()
            {
                // Act
                var section = _configurationService.GetSection<ApiMappingSection>("NonExistentSection");

                // Assert
                section.Should().NotBeNull();
                section.Should().BeOfType<ApiMappingSection>();
            }

            [Fact]
            public void Should_ReturnMessageQueueSection_WhenExists()
            {
                // Act
                var section = _configurationService.GetSection<MessageQueueSection>("messageQueue");

                // Assert
                section.Should().NotBeNull();
                section.Should().BeOfType<MessageQueueSection>();
            }
        }

        public class EncryptDecryptSection : ConfigurationServiceTests
        {
            [Fact]
            public void Should_HandleEncryptSection_Gracefully()
            {
                // Act & Assert - Should not throw
                var act = () => _configurationService.EncryptSection("appSettings");
                act.Should().NotThrow();
            }

            [Fact]
            public void Should_HandleDecryptSection_Gracefully()
            {
                // Act & Assert - Should not throw
                var act = () => _configurationService.DecryptSection("appSettings");
                act.Should().NotThrow();
            }

            [Fact]
            public void Should_HandleNonExistentSection_InEncrypt()
            {
                // Act & Assert - Should not throw
                var act = () => _configurationService.EncryptSection("NonExistentSection");
                act.Should().NotThrow();
            }

            [Fact]
            public void Should_HandleNonExistentSection_InDecrypt()
            {
                // Act & Assert - Should not throw
                var act = () => _configurationService.DecryptSection("NonExistentSection");
                act.Should().NotThrow();
            }
        }

        public class MessageQueueConfigProperty : ConfigurationServiceTests
        {
            [Fact]
            public void Should_ReturnValidMessageQueueConfig()
            {
                // Act
                var config = _configurationService.MessageQueueConfig;

                // Assert
                config.Should().NotBeNull();
                config.MaxQueueSize.Should().BeGreaterThan(0);
                config.BatchSize.Should().BeGreaterThan(0);
                config.RetryCount.Should().BeGreaterThanOrEqualTo(0);
                config.RetryInterval.Should().BeGreaterThan(0);
            }
        }

        public class ConfigurationChangedEvent : ConfigurationServiceTests
        {
            [Fact]
            public void Should_FireConfigurationChangedEvent_OnReload()
            {
                // Arrange
                var eventFired = false;
                ConfigurationChangedEventArgs? eventArgs = null;
                
                _configurationService.ConfigurationChanged += (sender, args) =>
                {
                    eventFired = true;
                    eventArgs = args;
                };

                // Act
                _configurationService.ReloadConfiguration();

                // Assert
                eventFired.Should().BeTrue();
                eventArgs.Should().NotBeNull();
                eventArgs!.SectionName.Should().NotBeNullOrEmpty();
                eventArgs.ChangeDescription.Should().NotBeNullOrEmpty();
            }

            [Fact]
            public void Should_ProvideEventArgs_WithCorrectInformation()
            {
                // Arrange
                ConfigurationChangedEventArgs? capturedArgs = null;
                _configurationService.ConfigurationChanged += (sender, args) => capturedArgs = args;

                // Act
                _configurationService.ReloadConfiguration();

                // Assert
                capturedArgs.Should().NotBeNull();
                capturedArgs!.SectionName.Should().Be("All");
                capturedArgs.ChangeDescription.Should().Be("Configuration reloaded");
            }
        }
    }
}