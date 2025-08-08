using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Configuration;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using System;
using System.Collections.Generic;

namespace SimpleSerialToApi.Tests.Mocks
{
    /// <summary>
    /// Mock configuration service for testing
    /// </summary>
    public class MockConfigurationService : IConfigurationService
    {
        private ApplicationConfiguration _applicationConfig;
        private readonly Dictionary<string, string> _appSettings;
        private readonly ILogger<MockConfigurationService> _logger;

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public MockConfigurationService(ILogger<MockConfigurationService> logger)
        {
            _logger = logger;
            _appSettings = CreateDefaultAppSettings();
            _applicationConfig = CreateDefaultApplicationConfiguration();
        }

        public ApplicationConfiguration ApplicationConfig => _applicationConfig;

        public IEnumerable<ApiEndpointConfig> ApiEndpoints => _applicationConfig.ApiEndpoints;

        public IEnumerable<MappingRule> MappingRules => _applicationConfig.MappingRules;

        public MessageQueueConfiguration MessageQueueConfig => _applicationConfig.MessageQueueSettings;

        public string GetAppSetting(string key)
        {
            return _appSettings.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public T GetSection<T>(string sectionName) where T : new()
        {
            // Return a new instance for testing - in real implementation this would deserialize from config
            return new T();
        }

        public bool ValidateConfiguration()
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(_applicationConfig.SerialSettings.PortName))
                    return false;

                if (_applicationConfig.SerialSettings.BaudRate <= 0)
                    return false;

                if (_applicationConfig.MessageQueueSettings.MaxQueueSize <= 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ReloadConfiguration()
        {
            _logger.LogInformation("Mock configuration reloaded");
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                SectionName = "All",
                ChangeDescription = "Configuration reloaded"
            });
        }

        public void EncryptSection(string sectionName)
        {
            _logger.LogInformation("Mock encrypt section: {SectionName}", sectionName);
        }

        public void DecryptSection(string sectionName)
        {
            _logger.LogInformation("Mock decrypt section: {SectionName}", sectionName);
        }

        /// <summary>
        /// Update application configuration for testing
        /// </summary>
        public void UpdateApplicationConfig(ApplicationConfiguration config)
        {
            _applicationConfig = config;
        }

        /// <summary>
        /// Update app setting for testing
        /// </summary>
        public void UpdateAppSetting(string key, string value)
        {
            _appSettings[key] = value;
        }

        /// <summary>
        /// Reset to default configuration
        /// </summary>
        public void Reset()
        {
            _appSettings.Clear();
            foreach (var setting in CreateDefaultAppSettings())
            {
                _appSettings[setting.Key] = setting.Value;
            }
            _applicationConfig = CreateDefaultApplicationConfiguration();
        }

        private Dictionary<string, string> CreateDefaultAppSettings()
        {
            return new Dictionary<string, string>
            {
                ["SerialPort"] = "COM1",
                ["BaudRate"] = "9600",
                ["DataBits"] = "8",
                ["Parity"] = "None",
                ["StopBits"] = "One",
                ["ReadTimeout"] = "5000",
                ["WriteTimeout"] = "5000",
                ["LogLevel"] = "Information",
                ["MaxQueueSize"] = "1000",
                ["BatchSize"] = "10",
                ["RetryCount"] = "3",
                ["RetryInterval"] = "1000"
            };
        }

        private ApplicationConfiguration CreateDefaultApplicationConfiguration()
        {
            return new ApplicationConfiguration
            {
                SerialSettings = new SerialConnectionSettings
                {
                    PortName = "COM1",
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = System.IO.Ports.Parity.None,
                    StopBits = System.IO.Ports.StopBits.One,
                    Handshake = System.IO.Ports.Handshake.None,
                    ReadTimeout = 5000,
                    WriteTimeout = 5000
                },
                MessageQueueSettings = new MessageQueueConfiguration
                {
                    MaxQueueSize = 1000,
                    BatchSize = 10,
                    RetryCount = 3,
                    RetryInterval = 1000
                },
                ApiEndpoints = new List<ApiEndpointConfig>
                {
                    new ApiEndpointConfig
                    {
                        Name = "TestEndpoint",
                        Url = "https://api.test.com/data",
                        Method = "POST",
                        Timeout = 30000
                    }
                },
                MappingRules = new List<MappingRule>
                {
                    new MappingRule
                    {
                        SourceField = "temperature",
                        TargetField = "temp",
                        DataType = "decimal"
                    }
                }
            };
        }

        public void Dispose()
        {
            // Nothing to dispose for mock
            GC.SuppressFinalize(this);
        }
    }
}