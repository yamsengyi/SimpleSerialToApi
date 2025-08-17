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
            _trayIconService?.Dispose();
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
