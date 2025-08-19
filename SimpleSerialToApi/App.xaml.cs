using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Services.Queues;
using SimpleSerialToApi.ViewModels;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Models;
using Serilog;
using System.Linq;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace SimpleSerialToApi
{
    public partial class App : WpfApplication
    {
        private ServiceProvider? _serviceProvider;
        private TrayIconService? _trayIconService;
        private bool _startMinimized = false;
        
        public ServiceProvider? ServiceProvider => _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // 명령행 인수 처리
                _startMinimized = e.Args.Contains("--minimized");

                // Serilog 설정
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Log.Information("Application starting... (StartMinimized: {StartMinimized})", _startMinimized);

                // USB 시리얼 통신을 위한 관리자 권한 자동 시작 등록 (작업 스케줄러 사용)
                try
                {
                    var startupService = new StartupService(Microsoft.Extensions.Logging.Abstractions.NullLogger<StartupService>.Instance);
                    
                    // 기존 레지스트리 방식 확인
                    var currentRegistryCommand = startupService.GetStartupCommand();
                    var isTaskSchedulerEnabled = startupService.IsStartupWithAdminEnabled();
                    
                    Log.Information("Startup status - Registry: {Registry}, TaskScheduler: {TaskScheduler}", 
                        currentRegistryCommand ?? "Not set", isTaskSchedulerEnabled);
                    
                    if (!isTaskSchedulerEnabled)
                    {
                        Log.Information("Registering for admin startup via Task Scheduler for USB serial access...");
                        var success = startupService.EnableStartupWithAdmin(true);
                        Log.Information("Task Scheduler registration result: {Success}", success);
                        
                        if (success)
                        {
                            Log.Information("Successfully registered for admin startup via Task Scheduler");
                        }
                        else
                        {
                            Log.Warning("Task Scheduler registration failed, falling back to registry method");
                            // Fallback to registry method if Task Scheduler fails
                            var registrySuccess = startupService.EnableStartup(true);
                            Log.Information("Registry fallback result: {Success}", registrySuccess);
                        }
                    }
                    else
                    {
                        Log.Information("Application already registered for admin startup via Task Scheduler");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to check/register admin startup, continuing anyway");
                }

                // 서비스 컨테이너 설정
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                Log.Information("Services configured successfully");

                // 트레이 아이콘 초기화
                _trayIconService = _serviceProvider.GetRequiredService<TrayIconService>();
                _trayIconService.Initialize();

                // 메인 윈도우 시작
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Log.Information("MainWindow created");
                
                // 트레이 아이콘에 윈도우 연결 (표시 전에 먼저 연결)
                _trayIconService.SetMainWindow(mainWindow);
                
                if (_startMinimized)
                {
                    // 최소화 상태로 시작 - 트레이에만 표시
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.ShowInTaskbar = false;
                    
                    // 윈도우를 숨긴 상태로 생성하고 트레이 아이콘 표시
                    mainWindow.Show(); // 초기화를 위해 한번 Show
                    mainWindow.Hide(); // 즉시 숨김
                    _trayIconService.Show();
                    _trayIconService.UpdateStatus(false, "프로그램이 트레이에서 실행 중입니다.");
                    
                    Log.Information("MainWindow started minimized to tray");
                }
                else
                {
                    mainWindow.Show();
                    Log.Information("MainWindow shown normally");
                }

                // 트레이 아이콘 이벤트 연결
                _trayIconService.ExitApplication += (s, e) => 
                {
                    Log.Information("Exit requested from tray icon");
                    Shutdown();
                };
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Application startup failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder => builder.AddSerilog());

            // HTTP Client 서비스 추가
            services.AddHttpClient();

            // COM 포트 검색 서비스
            services.AddSingleton<ComPortDiscoveryService>();

            // 설정 관리 서비스
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // 핵심 서비스들
            services.AddSingleton<SerialCommunicationService>();
            services.AddSingleton<SimpleQueueService>();
            services.AddSingleton<SimpleHttpService>();
            services.AddSingleton<TimerService>();

            // Queue Management System
            services.AddSingleton<IQueueManager, QueueManager>();
            services.AddSingleton<IQueueProcessor<MappedApiData>, ApiDataQueueProcessor>();
            services.AddSingleton<IApiClientService, HttpApiClientService>();
            services.AddSingleton<IApiClientFactory, ApiClientFactory>();

            // 새로 추가된 통신 기능 서비스들
            services.AddSingleton<ReservedWordService>();
            services.AddSingleton<DataMappingService>();
            services.AddSingleton<SerialMonitorService>();
            services.AddSingleton<ApiMonitorService>();
            services.AddSingleton<ApiFileLogService>();
            services.AddSingleton<SerialDataSimulator>();

            // 시스템 통합 서비스들
            services.AddSingleton<TrayIconService>();
            services.AddSingleton<StartupService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("Application shutting down...");

                // MainViewModel Dispose (through ServiceProvider) with timeout
                var mainViewModel = _serviceProvider?.GetService<MainViewModel>();
                if (mainViewModel != null)
                {
                    var disposeTask = Task.Run(() => mainViewModel.Dispose());
                    if (!disposeTask.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Log.Warning("MainViewModel disposal timed out");
                    }
                }

                // 트레이 아이콘 정리
                _trayIconService?.Dispose();

                // 서비스 컨테이너 정리
                _serviceProvider?.Dispose();

                // 강제 종료 처리
                Environment.Exit(0);

                Log.Information("Application shutdown complete");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application shutdown");
                // 강제 종료
                Environment.Exit(1);
            }
            finally
            {
                Log.CloseAndFlush();
                base.OnExit(e);
            }
        }
    }
}
