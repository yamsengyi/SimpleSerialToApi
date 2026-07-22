using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.Services.Queues;
using SimpleSerialToApi.Facades;
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
        private MainWindow? _mainWindow;
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
                // Console sink는 디버깅용으로만 사용하고, 운영 환경에서는 파일 로그만 사용
                // 실제 데이터 모니터링은 Serial/API Monitor 창을 통해 수행
                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File("logs/app.log", 
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");

#if DEBUG
                // 디버그 모드에서만 콘솔 출력 활성화
                logConfig = logConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
#endif

                Log.Logger = logConfig.CreateLogger();

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
                _mainWindow = mainWindow;
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

            // ─── Facades (리팩토링 v2: MainViewModel 책임 분산) ───
            services.AddSingleton<ISimulationFacade, SimulationFacade>();
            services.AddSingleton<IConfigurationFacade, ConfigurationFacade>();
            services.AddSingleton<IMonitorFacade, MonitorFacade>();
            services.AddSingleton<ISerialConnectionFacade, SerialConnectionFacade>();
            services.AddSingleton<IDataMappingFacade, DataMappingFacade>();
            services.AddSingleton<IWindowManagementFacade, WindowManagementFacade>();
            services.AddSingleton<IDataTransmissionFacade, DataTransmissionFacade>();

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

                if (_mainWindow?.DataContext is IDisposable disposableViewModel)
                {
                    var disposeTask = Task.Run(disposableViewModel.Dispose);
                    if (!disposeTask.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Log.Warning("MainViewModel disposal timed out");
                    }
                }

                // 트레이 아이콘 정리
                _trayIconService?.Dispose();

                // 서비스 컨테이너 정리
                _serviceProvider?.Dispose();

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
