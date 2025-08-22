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
        /// 작업 스케줄러를 통한 관리자 권한 자동 시작 등록
        /// </summary>
        /// <param name="startMinimized">최소화 상태로 시작 여부</param>
        /// <returns>성공 여부</returns>
        public bool EnableStartupWithAdmin(bool startMinimized = true)
        {
            try
            {
                // 실행 파일 경로 감지
                string? executablePath = null;
                try
                {
                    executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get MainModule path, using AppContext.BaseDirectory");
                }

                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    executablePath = Path.Combine(AppContext.BaseDirectory, "SimpleSerialToApi.exe");
                }

                var arguments = startMinimized ? "--minimized" : "";
                
                // 작업 스케줄러 XML 정의
                var taskXml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Date>{DateTime.Now:yyyy-MM-ddTHH:mm:ss}</Date>
    <Author>{Environment.UserName}</Author>
    <Description>SimpleSerialToApi Auto Start with Admin Rights</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <UserId>{Environment.UserDomainName}\\{Environment.UserName}</UserId>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>{Environment.UserDomainName}\\{Environment.UserName}</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{executablePath}</Command>
      <Arguments>{arguments}</Arguments>
    </Exec>
  </Actions>
</Task>";

                // 임시 XML 파일 생성
                var tempXmlPath = Path.GetTempFileName();
                File.WriteAllText(tempXmlPath, taskXml);

                try
                {
                    // schtasks 명령으로 작업 등록
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/create /tn \"SimpleSerialToApi\" /xml \"{tempXmlPath}\" /f",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(processInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();

                        if (process.ExitCode == 0)
                        {
                            // 기존 레지스트리 항목이 있다면 제거
                            DisableStartup();
                            
                            return true;
                        }
                        else
                        {
                            _logger.LogError("Task Scheduler registration failed. Exit code: {ExitCode}, Error: {Error}", process.ExitCode, error);
                            return false;
                        }
                    }
                }
                finally
                {
                    // 임시 파일 삭제
                    try { File.Delete(tempXmlPath); } catch { }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling admin startup via Task Scheduler");
                return false;
            }
        }

        /// <summary>
        /// 윈도우 시작시 자동 실행 등록 (일반 권한)
        /// </summary>
        /// <param name="startMinimized">최소화 상태로 시작 여부</param>
        /// <returns>성공 여부</returns>
        public bool EnableStartup(bool startMinimized = true)
        {
            try
            {
                // Windows 11 호환을 위한 실행 파일 경로 감지 (성공하는 앱 방식 적용)
                string? executablePath = null;
                try
                {
                    executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get MainModule path, falling back to alternatives");
                }

                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    // Single-file 배포 호환을 위해 AppContext.BaseDirectory 사용
                    var appDir = AppContext.BaseDirectory;
                    executablePath = Path.Combine(appDir, "SimpleSerialToApi.exe");
                }

                var arguments = startMinimized ? " --minimized" : "";
                var command = $"\"{executablePath}\"{arguments}";

                using var registryKey = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true);
                if (registryKey != null)
                {
                    registryKey.SetValue(APP_NAME, command);
                    
                    // 등록 직후 검증
                    var verifyValue = registryKey.GetValue(APP_NAME)?.ToString();
                    
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
        /// 작업 스케줄러에서 자동 시작 해제
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool DisableStartupWithAdmin()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = "/delete /tn \"SimpleSerialToApi\" /f",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();

                    if (process.ExitCode == 0 || output.Contains("지정한 파일을 찾을 수 없습니다") || output.Contains("does not exist"))
                    {
                        return true;
                    }
                    else
                    {
                        var error = process.StandardError.ReadToEnd();
                        _logger.LogError("Task Scheduler removal failed. Exit code: {ExitCode}, Error: {Error}", process.ExitCode, error);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling admin startup via Task Scheduler");
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
                    _logger.LogDebug("Startup status: {Status}, Value: {Value}", isEnabled ? "Enabled" : "Disabled", value?.ToString() ?? "null");
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
        /// 작업 스케줄러 등록 상태 확인
        /// </summary>
        /// <returns>등록 여부</returns>
        public bool IsStartupWithAdminEnabled()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = "/query /tn \"SimpleSerialToApi\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin startup status");
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

        /// <summary>
        /// Windows 11 시작 앱 상태 진단
        /// </summary>
        /// <returns>진단 정보</returns>
        public string DiagnoseStartupIssues()
        {
            var diagnostics = new System.Text.StringBuilder();
            
            try
            {
                // 1. 레지스트리 값 확인
                var command = GetStartupCommand();
                diagnostics.AppendLine($"Registry Command: {command ?? "Not found"}");
                
                // 2. 작업 스케줄러 확인
                var adminStartup = IsStartupWithAdminEnabled();
                diagnostics.AppendLine($"Task Scheduler (Admin): {(adminStartup ? "Enabled" : "Disabled")}");
                
                if (!string.IsNullOrEmpty(command))
                {
                    // 3. 실행 파일 존재 확인
                    var parts = command.Split(' ');
                    var exePath = parts[0].Trim('"');
                    var fileExists = File.Exists(exePath);
                    diagnostics.AppendLine($"Executable exists: {fileExists} ({exePath})");
                    
                    if (fileExists)
                    {
                        // 4. 파일 권한 확인
                        try
                        {
                            var fileInfo = new FileInfo(exePath);
                            diagnostics.AppendLine($"File size: {fileInfo.Length} bytes");
                            diagnostics.AppendLine($"Last modified: {fileInfo.LastWriteTime}");
                        }
                        catch (Exception ex)
                        {
                            diagnostics.AppendLine($"File access error: {ex.Message}");
                        }
                    }
                }
                
                // 5. Windows 11 시작 앱 설정 확인을 위한 안내
                diagnostics.AppendLine();
                diagnostics.AppendLine("Windows 11 추가 확인 사항:");
                diagnostics.AppendLine("1. 설정 > 앱 > 시작 프로그램에서 SimpleSerialToApi 상태 확인");
                diagnostics.AppendLine("2. Windows 보안 > 앱 및 브라우저 제어 > SmartScreen 설정 확인");
                diagnostics.AppendLine("3. 작업 관리자 > 시작프로그램 탭에서 상태 확인");
                diagnostics.AppendLine("4. USB 시리얼 통신을 위해서는 관리자 권한이 필요할 수 있음");
                
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"Diagnosis error: {ex.Message}");
            }
            
            return diagnostics.ToString();
        }
    }
}