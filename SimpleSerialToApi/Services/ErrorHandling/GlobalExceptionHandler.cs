using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services.Exceptions;
using SimpleSerialToApi.Services.Logging;
using SimpleSerialToApi.Services.Notifications;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;

namespace SimpleSerialToApi.Services.ErrorHandling
{
    /// <summary>
    /// Global exception handler for the application
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly Dictionary<Type, ExceptionHandler> _exceptionHandlers;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _exceptionHandlers = new Dictionary<Type, ExceptionHandler>();

            RegisterDefaultHandlers();
        }

        /// <summary>
        /// Handles unhandled exceptions from various contexts
        /// </summary>
        public void HandleUnhandledException(Exception exception, string context, object? sender = null)
        {
            try
            {
                _logger.LogCritical(exception, "Unhandled exception in {Context} from {Sender}", context, sender?.GetType().Name ?? "Unknown");

                // Try to get a specific handler
                var handler = GetExceptionHandler(exception.GetType());
                if (handler != null)
                {
                    handler.Handle(exception, context, sender);
                    return;
                }

                // Default handling
                HandleGenericException(exception, context, sender);
            }
            catch (Exception handlerException)
            {
                // Last resort - log to system event log if possible
                try
                {
                    var eventLog = new EventLog("Application");
                    eventLog.Source = "SimpleSerialToApi";
                    eventLog.WriteEntry(
                        $"Critical error in exception handler: {handlerException.Message}\nOriginal exception: {exception.Message}",
                        EventLogEntryType.Error);
                }
                catch
                {
                    // If we can't even log to event log, there's not much more we can do
                    Debug.WriteLine($"CRITICAL: Exception handler failed: {handlerException}");
                    Debug.WriteLine($"Original exception: {exception}");
                }
            }
        }

        /// <summary>
        /// Handles application domain unhandled exceptions
        /// </summary>
        public void HandleAppDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleUnhandledException(exception, "AppDomain", sender);

                if (e.IsTerminating)
                {
                    _logger.LogCritical("Application is terminating due to unhandled exception");
                    _notificationService.ShowError("애플리케이션에 치명적인 오류가 발생했습니다. 프로그램을 종료합니다.", exception);
                }
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in tasks
        /// </summary>
        public void HandleTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Unobserved task exception");
            
            foreach (var innerException in e.Exception.InnerExceptions)
            {
                HandleUnhandledException(innerException, "UnobservedTask", sender);
            }

            // Mark as observed to prevent process termination
            e.SetObserved();
        }

        /// <summary>
        /// Registers a custom exception handler for a specific exception type
        /// </summary>
        public void RegisterExceptionHandler<T>(ExceptionHandler handler) where T : Exception
        {
            _exceptionHandlers[typeof(T)] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Generates an error report for the exception
        /// </summary>
        public ErrorReport GenerateErrorReport(Exception exception, string context, object? sender = null)
        {
            return new ErrorReport
            {
                Timestamp = DateTime.UtcNow,
                ExceptionType = exception.GetType().FullName ?? "Unknown",
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty,
                Context = context,
                SenderType = sender?.GetType().FullName,
                InnerExceptions = GetInnerExceptionDetails(exception),
                SystemInfo = GetSystemInfo(),
                ApplicationInfo = GetApplicationInfo()
            };
        }

        private void RegisterDefaultHandlers()
        {
            // Serial Communication Exception Handler
            _exceptionHandlers[typeof(SerialCommunicationException)] = new ExceptionHandler(
                "SerialCommunication",
                (ex, context, sender) =>
                {
                    var serialEx = (SerialCommunicationException)ex;
                    _logger.LogSerialError(serialEx.PortName, "Exception", serialEx, serialEx.AdditionalData);
                    _notificationService.ShowError($"시리얼 통신 오류: {serialEx.ErrorType} (포트: {serialEx.PortName})", serialEx);
                }
            );

            // API Communication Exception Handler
            _exceptionHandlers[typeof(ApiCommunicationException)] = new ExceptionHandler(
                "ApiCommunication",
                (ex, context, sender) =>
                {
                    var apiEx = (ApiCommunicationException)ex;
                    _logger.LogApiError(apiEx.EndpointUrl, apiEx.HttpMethod, apiEx, apiEx.StatusCode, apiEx.ResponseContent);
                    _notificationService.ShowError($"API 통신 오류: {apiEx.EndpointName}", apiEx);
                }
            );

            // Configuration Exception Handler
            _exceptionHandlers[typeof(ConfigurationException)] = new ExceptionHandler(
                "Configuration",
                (ex, context, sender) =>
                {
                    var configEx = (ConfigurationException)ex;
                    _logger.LogConfigurationError(configEx.SectionName, configEx.SettingName, configEx);
                    _notificationService.ShowError($"설정 오류: {configEx.SectionName}.{configEx.SettingName}", configEx);
                }
            );

            // Queue Operation Exception Handler
            _exceptionHandlers[typeof(QueueOperationException)] = new ExceptionHandler(
                "QueueOperation",
                (ex, context, sender) =>
                {
                    var queueEx = (QueueOperationException)ex;
                    _logger.LogQueueError(queueEx.QueueName, queueEx.Operation, queueEx, queueEx.MessageCount);
                    _notificationService.ShowError($"큐 작업 오류: {queueEx.QueueName} - {queueEx.Operation}", queueEx);
                }
            );

            // Data Parsing Exception Handler
            _exceptionHandlers[typeof(DataParsingException)] = new ExceptionHandler(
                "DataParsing",
                (ex, context, sender) =>
                {
                    var parseEx = (DataParsingException)ex;
                    _logger.LogError(parseEx, "Data parsing failed for format {DataFormat} at position {Position}", 
                        parseEx.DataFormat, parseEx.DataPosition);
                    _notificationService.ShowWarning($"데이터 파싱 오류: {parseEx.DataFormat} 형식");
                }
            );

            // Timeout Exception Handler
            _exceptionHandlers[typeof(TimeoutException)] = new ExceptionHandler(
                "Timeout",
                (ex, context, sender) =>
                {
                    _logger.LogWarning(ex, "Operation timed out in {Context}", context);
                    _notificationService.ShowWarning($"작업 시간 초과: {context}");
                }
            );

            // Argument Exception Handler
            _exceptionHandlers[typeof(ArgumentException)] = new ExceptionHandler(
                "ArgumentValidation",
                (ex, context, sender) =>
                {
                    _logger.LogWarning(ex, "Argument validation failed in {Context}", context);
                    _notificationService.ShowWarning("잘못된 매개변수가 전달되었습니다.");
                }
            );

            // File/IO Exception Handler
            _exceptionHandlers[typeof(IOException)] = new ExceptionHandler(
                "IO",
                (ex, context, sender) =>
                {
                    _logger.LogError(ex, "I/O operation failed in {Context}", context);
                    _notificationService.ShowError("파일 또는 네트워크 작업 중 오류가 발생했습니다.", ex);
                }
            );
        }

        private ExceptionHandler? GetExceptionHandler(Type exceptionType)
        {
            // Direct match
            if (_exceptionHandlers.TryGetValue(exceptionType, out var handler))
            {
                return handler;
            }

            // Check inheritance hierarchy
            var currentType = exceptionType.BaseType;
            while (currentType != null)
            {
                if (_exceptionHandlers.TryGetValue(currentType, out handler))
                {
                    return handler;
                }
                currentType = currentType.BaseType;
            }

            return null;
        }

        private void HandleGenericException(Exception exception, string context, object? sender)
        {
            var errorReport = GenerateErrorReport(exception, context, sender);
            
            _logger.LogError(exception, "Generic exception handling for {ExceptionType} in {Context}", 
                exception.GetType().Name, context);

            // Show user-friendly message based on exception type
            var userMessage = GetUserFriendlyMessage(exception, context);
            _notificationService.ShowError(userMessage, exception);
        }

        private string GetUserFriendlyMessage(Exception exception, string context)
        {
            return exception switch
            {
                OutOfMemoryException => "메모리가 부족합니다. 일부 작업을 중단하거나 애플리케이션을 재시작하세요.",
                StackOverflowException => "프로그램 실행 중 스택 오버플로가 발생했습니다.",
                UnauthorizedAccessException => "필요한 권한이 없습니다. 관리자로 실행하거나 권한을 확인하세요.",
                NotSupportedException => "지원되지 않는 작업입니다.",
                InvalidOperationException => "현재 상태에서는 이 작업을 수행할 수 없습니다.",
                ArgumentNullException => "필수 데이터가 누락되었습니다.",
                FormatException => "데이터 형식이 올바르지 않습니다.",
                _ => $"예기치 않은 오류가 발생했습니다: {context}"
            };
        }

        private List<InnerExceptionDetail> GetInnerExceptionDetails(Exception exception)
        {
            var details = new List<InnerExceptionDetail>();
            var current = exception.InnerException;

            while (current != null)
            {
                details.Add(new InnerExceptionDetail
                {
                    ExceptionType = current.GetType().FullName ?? "Unknown",
                    Message = current.Message,
                    StackTrace = current.StackTrace ?? string.Empty
                });
                current = current.InnerException;
            }

            return details;
        }

        private SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                OSVersion = Environment.OSVersion.ToString(),
                CLRVersion = Environment.Version.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                TickCount = Environment.TickCount64
            };
        }

        private ApplicationInfo GetApplicationInfo()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            return new ApplicationInfo
            {
                ApplicationName = assembly?.GetName().Name ?? "SimpleSerialToApi",
                Version = assembly?.GetName().Version?.ToString() ?? "Unknown",
                Location = assembly?.Location ?? "Unknown",
                StartTime = Process.GetCurrentProcess().StartTime,
                CurrentDirectory = Environment.CurrentDirectory
            };
        }
    }

    /// <summary>
    /// Handler for specific exception types
    /// </summary>
    public class ExceptionHandler
    {
        public string Name { get; }
        public Action<Exception, string, object?> HandleAction { get; }

        public ExceptionHandler(string name, Action<Exception, string, object?> handleAction)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            HandleAction = handleAction ?? throw new ArgumentNullException(nameof(handleAction));
        }

        public void Handle(Exception exception, string context, object? sender)
        {
            HandleAction(exception, context, sender);
        }
    }

    /// <summary>
    /// Error report generated for exceptions
    /// </summary>
    public class ErrorReport
    {
        public DateTime Timestamp { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string? SenderType { get; set; }
        public List<InnerExceptionDetail> InnerExceptions { get; set; } = new();
        public SystemInfo SystemInfo { get; set; } = new();
        public ApplicationInfo ApplicationInfo { get; set; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Error Report - {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Context: {Context}");
            sb.AppendLine($"Exception Type: {ExceptionType}");
            sb.AppendLine($"Message: {Message}");
            sb.AppendLine($"Sender: {SenderType}");
            sb.AppendLine();
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(StackTrace);

            if (InnerExceptions.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Inner Exceptions:");
                for (int i = 0; i < InnerExceptions.Count; i++)
                {
                    var inner = InnerExceptions[i];
                    sb.AppendLine($"  {i + 1}. {inner.ExceptionType}: {inner.Message}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Application: {ApplicationInfo.ApplicationName} v{ApplicationInfo.Version}");
            sb.AppendLine($"System: {SystemInfo.OSVersion}");
            sb.AppendLine($"Machine: {SystemInfo.MachineName}");

            return sb.ToString();
        }
    }

    public class InnerExceptionDetail
    {
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }

    public class SystemInfo
    {
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string CLRVersion { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public long WorkingSet { get; set; }
        public long TickCount { get; set; }
    }

    public class ApplicationInfo
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string CurrentDirectory { get; set; } = string.Empty;
    }
}