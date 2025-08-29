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
        private ApplicationConfig _applicationConfig;
        private readonly object _configLock = new object();

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _applicationConfig = new ApplicationConfig();
            
            LoadConfiguration();
            
            // 파일 시스템 와처를 제거하여 파일 잠금 문제를 방지
            // 설정 변경은 명시적인 ReloadConfiguration() 호출로만 처리
        }

        public ApplicationConfig ApplicationConfig => _applicationConfig;

        public IEnumerable<ApiEndpointConfig> ApiEndpoints => _applicationConfig.ApiEndpoints;

        public IEnumerable<MappingRuleConfig> MappingRules => _applicationConfig.MappingRules;

        public MessageQueueConfig MessageQueueConfig => _applicationConfig.MessageQueueSettings;

        public T GetSection<T>(string sectionName) where T : class, new()
        {
            System.Configuration.Configuration? tempConfig = null;
            try
            {
                lock (_configLock)
                {
                    // 설정 파일을 일시적으로 열고 섹션을 읽은 후 즉시 닫기
                    tempConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var section = tempConfig?.GetSection(sectionName) as T;
                    return section ?? new T();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration section {SectionName}", sectionName);
                return new T();
            }
            finally
            {
                // 설정 파일 참조를 즉시 해제
                tempConfig = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting section {SectionName}", sectionName);
            }
        }

        private void LoadConfiguration()
        {
            System.Configuration.Configuration? tempConfig = null;
            try
            {
                // 설정 파일을 일시적으로 열고 즉시 처리
                tempConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
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

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
            }
            finally
            {
                // 설정 파일 참조를 즉시 해제
                tempConfig = null;
                // 가비지 컬렉션을 통해 파일 핸들 정리
                GC.Collect();
                GC.WaitForPendingFinalizers();
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
                
            // 버퍼 크기 설정 로드
            if (int.TryParse(GetAppSetting("ReadBufferSize"), out var readBufferSize))
                _applicationConfig.SerialSettings.ReadBufferSize = readBufferSize;

            if (int.TryParse(GetAppSetting("WriteBufferSize"), out var writeBufferSize))
                _applicationConfig.SerialSettings.WriteBufferSize = writeBufferSize;
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

        /// <summary>
        /// Save serial connection settings to App.config
        /// </summary>
        public void SaveSerialSettings(SerialConnectionSettings settings)
        {
            System.Configuration.Configuration? tempConfig = null;
            try
            {
                lock (_configLock)
                {
                    // 설정 파일을 일시적으로 열어서 변경 후 즉시 저장하고 닫기
                    tempConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    
                    if (tempConfig != null)
                    {
                        SetAppSettingInConfig(tempConfig, "SerialPort", settings.PortName);
                        SetAppSettingInConfig(tempConfig, "BaudRate", settings.BaudRate.ToString());
                        SetAppSettingInConfig(tempConfig, "Parity", settings.Parity.ToString());
                        SetAppSettingInConfig(tempConfig, "DataBits", settings.DataBits.ToString());
                        SetAppSettingInConfig(tempConfig, "StopBits", settings.StopBits.ToString());
                        SetAppSettingInConfig(tempConfig, "Handshake", settings.Handshake.ToString());
                        SetAppSettingInConfig(tempConfig, "ReadTimeout", settings.ReadTimeout.ToString());
                        SetAppSettingInConfig(tempConfig, "WriteTimeout", settings.WriteTimeout.ToString());
                        SetAppSettingInConfig(tempConfig, "ReadBufferSize", settings.ReadBufferSize.ToString());
                        SetAppSettingInConfig(tempConfig, "WriteBufferSize", settings.WriteBufferSize.ToString());

                        tempConfig.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");

                        // Update local settings
                        _applicationConfig.SerialSettings = settings;

                        // Notify listeners of configuration change
                        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs 
                        { 
                            SectionName = "SerialSettings", 
                            ChangeDescription = "Serial communication settings updated" 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving serial settings");
                throw;
            }
            finally
            {
                // 설정 파일 참조를 즉시 해제
                tempConfig = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Set an application setting value in a specific configuration instance
        /// </summary>
        private void SetAppSettingInConfig(System.Configuration.Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }
        }

        /// <summary>
        /// Set an application setting value (deprecated - use SetAppSettingInConfig with temporary config)
        /// </summary>
        private void SetAppSetting(string key, string value)
        {
            // 이 메서드는 더 이상 사용되지 않으므로 빈 구현으로 유지
            // 대신 SetAppSettingInConfig를 사용하여 임시 설정 객체로 작업
            _logger.LogWarning("SetAppSetting called but deprecated. Use SetAppSettingInConfig instead.");
        }

        public void Dispose()
        {
            // 파일 와처가 제거되었으므로 정리할 리소스가 없음
            // 필요시 다른 리소스 정리 코드 추가
        }
    }
}