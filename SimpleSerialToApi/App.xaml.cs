using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Services.ErrorHandling;
using SimpleSerialToApi.Services.Notifications;
using SimpleSerialToApi.Services.Recovery;
using SimpleSerialToApi.Services.Monitoring;
using SimpleSerialToApi.Services.Diagnostics;
using SimpleSerialToApi.ViewModels;
using Serilog;
using System.IO;

namespace SimpleSerialToApi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private GlobalExceptionHandler? _globalExceptionHandler;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure Serilog early
            ConfigureSerilog();

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // Setup global exception handling
                SetupGlobalExceptionHandling();

                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogInformation("Application starting up");

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                // Start health monitoring
                var healthMonitor = _serviceProvider.GetService<IHealthMonitor>();
                if (healthMonitor != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await healthMonitor.StartMonitoringAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to start health monitoring");
                        }
                    });
                }

                base.OnStartup(e);
            }
            catch (Exception startupException)
            {
                // Last resort error handling during startup
                MessageBox.Show($"애플리케이션 시작 중 치명적인 오류가 발생했습니다:\n{startupException.Message}", 
                    "시작 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                
                Shutdown(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure logging with Serilog
            services.AddLogging(configure => 
            {
                configure.ClearProviders();
                configure.AddSerilog();
            });
            
            // Register Configuration Service
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Register Serial Communication Service
            services.AddSingleton<ISerialCommunicationService, SerialCommunicationService>();
            
            // Register API Services
            services.AddSingleton<IHttpApiClientService, HttpApiClientService>();
            
            // Register Queue Services
            services.AddSingleton<IQueueManager, Services.Queues.QueueManager>();
            
            // Register Error Handling Services
            services.AddSingleton<INotificationService, WpfNotificationService>();
            services.AddSingleton<GlobalExceptionHandler>();
            
            // Register Recovery Strategies
            services.AddTransient<IRecoveryStrategy<bool>, SerialConnectionRecoveryStrategy>();
            services.AddTransient<IRecoveryStrategy<bool>, ApiConnectionRecoveryStrategy>();
            services.AddSingleton<RecoveryManager>();
            
            // Register Health Monitoring
            services.AddTransient<IHealthChecker, SerialHealthChecker>();
            services.AddTransient<IHealthChecker, ApiHealthChecker>();
            services.AddTransient<IHealthChecker, QueueHealthChecker>();
            services.AddTransient<IHealthChecker, SystemResourceHealthChecker>();
            services.AddSingleton<IHealthMonitor, ApplicationHealthMonitor>();
            
            // Register Diagnostics
            services.AddSingleton<DiagnosticReportGenerator>();
            
            // Register HTTP Client for API health checks
            services.AddHttpClient();
            
            // Register MVVM infrastructure
            services.AddSingleton<IMessenger, Messenger>();
            
            // Register ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SerialStatusViewModel>();
            services.AddTransient<ApiStatusViewModel>();
            services.AddTransient<QueueStatusViewModel>();
            services.AddTransient<SettingsViewModel>();
            
            // Register main window
            services.AddTransient<MainWindow>();
        }

        private void ConfigureSerilog()
        {
            // Ensure logs directory exists
            Directory.CreateDirectory("logs");

            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.File(
                    path: "logs/app_.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug();

            Log.Logger = logConfig.CreateLogger();
        }

        private void SetupGlobalExceptionHandling()
        {
            if (_serviceProvider == null)
                return;

            _globalExceptionHandler = _serviceProvider.GetRequiredService<GlobalExceptionHandler>();

            // Handle unhandled exceptions in the current AppDomain
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                _globalExceptionHandler.HandleAppDomainException(sender, e);
            };

            // Handle unhandled exceptions in tasks
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                _globalExceptionHandler.HandleTaskException(sender, e);
            };

            // Handle unhandled exceptions in the WPF application
            this.DispatcherUnhandledException += (sender, e) =>
            {
                try
                {
                    _globalExceptionHandler.HandleUnhandledException(e.Exception, "WPF Dispatcher", sender);
                    e.Handled = true; // Mark as handled to prevent application crash
                }
                catch (Exception handlerException)
                {
                    // If even the exception handler fails, let WPF handle it
                    var logger = _serviceProvider?.GetService<ILogger<App>>();
                    logger?.LogCritical(handlerException, "Global exception handler itself failed");
                }
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var logger = _serviceProvider?.GetService<ILogger<App>>();
                logger?.LogInformation("Application shutting down");

                // Stop health monitoring
                var healthMonitor = _serviceProvider?.GetService<IHealthMonitor>();
                if (healthMonitor != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await healthMonitor.StopMonitoringAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Error stopping health monitoring during shutdown");
                        }
                    });
                }

                _serviceProvider?.Dispose();
                Log.CloseAndFlush();
            }
            catch (Exception shutdownException)
            {
                // Log to Windows Event Log as last resort
                try
                {
                    System.Diagnostics.EventLog.WriteEntry("SimpleSerialToApi", 
                        $"Error during application shutdown: {shutdownException}", 
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch
                {
                    // Nothing more we can do
                }
            }

            base.OnExit(e);
        }
    }
}