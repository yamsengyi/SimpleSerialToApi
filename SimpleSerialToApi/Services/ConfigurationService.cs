using System.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Configuration;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// Configuration management service with hot reload and validation
    /// </summary>
    public class ConfigurationService : IConfigurationService, IDisposable
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly FileSystemWatcher? _configWatcher;
        private System.Configuration.Configuration? _configuration;
        private ApplicationConfig _applicationConfig;
        private readonly object _configLock = new object();

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _applicationConfig = new ApplicationConfig();
            
            LoadConfiguration();
            
            // Setup file system watcher for hot reload
            try
            {
                // In .NET Core/.NET 5+, look for config file in the application directory
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var configFilePath = Path.Combine(baseDirectory, "App.config");
                
                // Also try looking for the executable config file
                if (!File.Exists(configFilePath))
                {
                    var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        configFilePath = exePath + ".config";
                    }
                }
                
                if (File.Exists(configFilePath))
                {
                    var directory = Path.GetDirectoryName(configFilePath);
                    var fileName = Path.GetFileName(configFilePath);
                    
                    if (!string.IsNullOrEmpty(directory))
                    {
                        _configWatcher = new FileSystemWatcher(directory, fileName)
                        {
                            EnableRaisingEvents = true,
                            NotifyFilter = NotifyFilters.LastWrite
                        };
                        
                        _configWatcher.Changed += OnConfigFileChanged;
                        _logger.LogInformation("Configuration file watcher initialized for {ConfigFile}", configFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize configuration file watcher");
            }
        }

        public ApplicationConfig ApplicationConfig => _applicationConfig;

        public IEnumerable<ApiEndpointConfig> ApiEndpoints => _applicationConfig.ApiEndpoints;

        public IEnumerable<MappingRuleConfig> MappingRules => _applicationConfig.MappingRules;

        public MessageQueueConfig MessageQueueConfig => _applicationConfig.MessageQueueSettings;

        public T GetSection<T>(string sectionName) where T : class, new()
        {
            try
            {
                lock (_configLock)
                {
                    var section = _configuration?.GetSection(sectionName) as T;
                    return section ?? new T();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration section {SectionName}", sectionName);
                return new T();
            }
        }

        public string GetAppSetting(string key)
        {
            try
            {
                return ConfigurationManager.AppSettings[key] ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting app setting {Key}", key);
                return string.Empty;
            }
        }

        public void ReloadConfiguration()
        {
            try
            {
                lock (_configLock)
                {
                    ConfigurationManager.RefreshSection("appSettings");
                    LoadConfiguration();
                    _logger.LogInformation("Configuration reloaded successfully");
                    
                    ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                    {
                        SectionName = "All",
                        ChangeDescription = "Configuration reloaded"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration");
            }
        }

        public bool ValidateConfiguration()
        {
            try
            {
                var isValid = true;
                var errors = new List<string>();

                // Validate serial settings
                if (string.IsNullOrEmpty(_applicationConfig.SerialSettings.PortName))
                {
                    errors.Add("SerialPort is required");
                    isValid = false;
                }

                if (_applicationConfig.SerialSettings.BaudRate <= 0)
                {
                    errors.Add("BaudRate must be greater than 0");
                    isValid = false;
                }

                // Validate API endpoints
                foreach (var endpoint in _applicationConfig.ApiEndpoints)
                {
                    if (string.IsNullOrEmpty(endpoint.Name))
                    {
                        errors.Add("API endpoint name is required");
                        isValid = false;
                    }

                    if (string.IsNullOrEmpty(endpoint.Url) || !Uri.IsWellFormedUriString(endpoint.Url, UriKind.Absolute))
                    {
                        errors.Add($"Invalid URL for endpoint {endpoint.Name}");
                        isValid = false;
                    }

                    if (endpoint.Timeout <= 0)
                    {
                        errors.Add($"Timeout must be greater than 0 for endpoint {endpoint.Name}");
                        isValid = false;
                    }
                }

                // Validate message queue settings
                if (_applicationConfig.MessageQueueSettings.MaxQueueSize <= 0)
                {
                    errors.Add("MaxQueueSize must be greater than 0");
                    isValid = false;
                }

                // Validate mapping rules
                foreach (var rule in _applicationConfig.MappingRules)
                {
                    if (string.IsNullOrEmpty(rule.SourceField))
                    {
                        errors.Add("SourceField is required for mapping rules");
                        isValid = false;
                    }

                    if (string.IsNullOrEmpty(rule.TargetField))
                    {
                        errors.Add($"TargetField is required for mapping rule {rule.SourceField}");
                        isValid = false;
                    }
                }

                if (!isValid)
                {
                    _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", errors));
                }
                else
                {
                    _logger.LogInformation("Configuration validation passed");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                return false;
            }
        }

        public void EncryptSection(string sectionName)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.GetSection(sectionName);
                
                if (section != null && !section.SectionInformation.IsProtected)
                {
                    section.SectionInformation.ProtectSection("RsaProtectedConfigurationProvider");
                    config.Save();
                    ConfigurationManager.RefreshSection(sectionName);
                    _logger.LogInformation("Section {SectionName} encrypted successfully", sectionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting section {SectionName}", sectionName);
            }
        }

        public void DecryptSection(string sectionName)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.GetSection(sectionName);
                
                if (section != null && section.SectionInformation.IsProtected)
                {
                    section.SectionInformation.UnprotectSection();
                    config.Save();
                    ConfigurationManager.RefreshSection(sectionName);
                    _logger.LogInformation("Section {SectionName} decrypted successfully", sectionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting section {SectionName}", sectionName);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // Load application configuration
                _applicationConfig = new ApplicationConfig();

                // Load serial settings from appSettings
                LoadSerialSettings();
                
                // Load API endpoints from custom section
                LoadApiEndpoints();
                
                // Load mapping rules from custom section
                LoadMappingRules();
                
                // Load message queue settings from custom section
                LoadMessageQueueSettings();
                
                // Load logging level
                _applicationConfig.LogLevel = GetAppSetting("LogLevel");

                _logger.LogInformation("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
            }
        }

        private void LoadSerialSettings()
        {
            _applicationConfig.SerialSettings.PortName = GetAppSetting("SerialPort");
            
            if (int.TryParse(GetAppSetting("BaudRate"), out var baudRate))
                _applicationConfig.SerialSettings.BaudRate = baudRate;

            if (Enum.TryParse<System.IO.Ports.Parity>(GetAppSetting("Parity"), out var parity))
                _applicationConfig.SerialSettings.Parity = parity;

            if (int.TryParse(GetAppSetting("DataBits"), out var dataBits))
                _applicationConfig.SerialSettings.DataBits = dataBits;

            if (Enum.TryParse<System.IO.Ports.StopBits>(GetAppSetting("StopBits"), out var stopBits))
                _applicationConfig.SerialSettings.StopBits = stopBits;

            if (Enum.TryParse<System.IO.Ports.Handshake>(GetAppSetting("Handshake"), out var handshake))
                _applicationConfig.SerialSettings.Handshake = handshake;

            if (int.TryParse(GetAppSetting("ReadTimeout"), out var readTimeout))
                _applicationConfig.SerialSettings.ReadTimeout = readTimeout;

            if (int.TryParse(GetAppSetting("WriteTimeout"), out var writeTimeout))
                _applicationConfig.SerialSettings.WriteTimeout = writeTimeout;
        }

        private void LoadApiEndpoints()
        {
            try
            {
                var apiMappingSection = GetSection<ApiMappingSection>("apiMappings");
                _applicationConfig.ApiEndpoints.Clear();

                if (apiMappingSection?.Endpoints != null)
                {
                    foreach (ApiEndpointElement endpoint in apiMappingSection.Endpoints)
                    {
                        _applicationConfig.ApiEndpoints.Add(new ApiEndpointConfig
                        {
                            Name = endpoint.Name,
                            Url = endpoint.Url,
                            Method = endpoint.Method,
                            AuthType = endpoint.AuthType,
                            AuthToken = endpoint.AuthToken,
                            Timeout = endpoint.Timeout
                        });
                    }
                }

                _logger.LogDebug("Loaded {Count} API endpoints", _applicationConfig.ApiEndpoints.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading API endpoints");
            }
        }

        private void LoadMappingRules()
        {
            try
            {
                var apiMappingSection = GetSection<ApiMappingSection>("apiMappings");
                _applicationConfig.MappingRules.Clear();

                if (apiMappingSection?.MappingRules != null)
                {
                    foreach (MappingRuleElement rule in apiMappingSection.MappingRules)
                    {
                        _applicationConfig.MappingRules.Add(new MappingRuleConfig
                        {
                            SourceField = rule.SourceField,
                            TargetField = rule.TargetField,
                            DataType = rule.DataType,
                            Converter = rule.Converter,
                            DefaultValue = rule.DefaultValue,
                            IsRequired = rule.IsRequired
                        });
                    }
                }

                _logger.LogDebug("Loaded {Count} mapping rules", _applicationConfig.MappingRules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading mapping rules");
            }
        }

        private void LoadMessageQueueSettings()
        {
            try
            {
                var messageQueueSection = GetSection<MessageQueueSection>("messageQueue");
                
                if (messageQueueSection != null)
                {
                    _applicationConfig.MessageQueueSettings.MaxQueueSize = messageQueueSection.MaxQueueSize;
                    _applicationConfig.MessageQueueSettings.BatchSize = messageQueueSection.BatchSize;
                    _applicationConfig.MessageQueueSettings.RetryCount = messageQueueSection.RetryCount;
                    _applicationConfig.MessageQueueSettings.RetryInterval = messageQueueSection.RetryInterval;
                }

                _logger.LogDebug("Loaded message queue settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading message queue settings");
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce rapid file changes
            Task.Delay(500).ContinueWith(_ =>
            {
                try
                {
                    _logger.LogInformation("Configuration file changed, reloading...");
                    ReloadConfiguration();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling configuration file change");
                }
            });
        }

        public void Dispose()
        {
            _configWatcher?.Dispose();
        }
    }
}