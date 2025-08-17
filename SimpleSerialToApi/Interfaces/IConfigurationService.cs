using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Interfaces
{
    /// <summary>
    /// Event arguments for configuration change events
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string SectionName { get; set; } = string.Empty;
        public string ChangeDescription { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for configuration management service
    /// </summary>
    public interface IConfigurationService : IDisposable
    {
        /// <summary>
        /// Event raised when configuration changes are detected
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// Gets a specific configuration section
        /// </summary>
        /// <typeparam name="T">Type of configuration section</typeparam>
        /// <param name="sectionName">Name of the section</param>
        /// <returns>Configuration section or new instance if not found</returns>
        T GetSection<T>(string sectionName) where T : class, new();

        /// <summary>
        /// Gets an application setting value
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value or empty string if not found</returns>
        string GetAppSetting(string key);

        /// <summary>
        /// Reloads configuration from the source
        /// </summary>
        void ReloadConfiguration();

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfiguration();

        /// <summary>
        /// Encrypts a configuration section
        /// </summary>
        /// <param name="sectionName">Name of the section to encrypt</param>
        void EncryptSection(string sectionName);

        /// <summary>
        /// Decrypts a configuration section
        /// </summary>
        /// <param name="sectionName">Name of the section to decrypt</param>
        void DecryptSection(string sectionName);

        /// <summary>
        /// Gets the application configuration
        /// </summary>
        ApplicationConfig ApplicationConfig { get; }

        /// <summary>
        /// Gets API endpoint configurations
        /// </summary>
        IEnumerable<ApiEndpointConfig> ApiEndpoints { get; }

        /// <summary>
        /// Gets API mapping rules
        /// </summary>
        IEnumerable<MappingRuleConfig> MappingRules { get; }

        /// <summary>
        /// Gets message queue configuration
        /// </summary>
        MessageQueueConfig MessageQueueConfig { get; }

        /// <summary>
        /// Save serial connection settings to configuration
        /// </summary>
        /// <param name="settings">Serial connection settings to save</param>
        void SaveSerialSettings(SerialConnectionSettings settings);
    }
}