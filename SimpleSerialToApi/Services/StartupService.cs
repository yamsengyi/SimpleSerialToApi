using System;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.IO;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 윈도우 시작 프로그램 관리 서비스
    /// </summary>
    public class StartupService
    {
        private readonly ILogger<StartupService> _logger;
        private const string REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "SimpleSerialToApi";

        public StartupService(ILogger<StartupService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 윈도우 시작시 자동 실행 등록
        /// </summary>
        /// <param name="startMinimized">최소화 상태로 시작 여부</param>
        /// <returns>성공 여부</returns>
        public bool EnableStartup(bool startMinimized = true)
        {
            try
            {
                var executablePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                var arguments = startMinimized ? " --minimized" : "";
                var command = $"\"{executablePath}\"{arguments}";

                using var registryKey = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true);
                if (registryKey != null)
                {
                    registryKey.SetValue(APP_NAME, command);
                    _logger.LogInformation("Startup enabled: {Command}", command);
                    return true;
                }
                else
                {
                    _logger.LogError("Could not access registry key for startup registration");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling startup");
                return false;
            }
        }

        /// <summary>
        /// 윈도우 시작시 자동 실행 해제
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool DisableStartup()
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true);
                if (registryKey != null)
                {
                    registryKey.DeleteValue(APP_NAME, false);
                    _logger.LogInformation("Startup disabled");
                    return true;
                }
                else
                {
                    _logger.LogError("Could not access registry key for startup removal");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling startup");
                return false;
            }
        }

        /// <summary>
        /// 시작 프로그램 등록 상태 확인
        /// </summary>
        /// <returns>등록 여부</returns>
        public bool IsStartupEnabled()
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, false);
                if (registryKey != null)
                {
                    var value = registryKey.GetValue(APP_NAME);
                    var isEnabled = value != null;
                    _logger.LogDebug("Startup status: {Status}", isEnabled ? "Enabled" : "Disabled");
                    return isEnabled;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking startup status");
                return false;
            }
        }

        /// <summary>
        /// 현재 등록된 시작 프로그램 명령어 가져오기
        /// </summary>
        /// <returns>등록된 명령어 또는 null</returns>
        public string? GetStartupCommand()
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, false);
                if (registryKey != null)
                {
                    var value = registryKey.GetValue(APP_NAME)?.ToString();
                    _logger.LogDebug("Current startup command: {Command}", value ?? "Not set");
                    return value;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting startup command");
                return null;
            }
        }
    }
}
