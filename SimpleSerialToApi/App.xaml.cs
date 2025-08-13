using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.ViewModels;
using Serilog;

namespace SimpleSerialToApi
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        
        public ServiceProvider? ServiceProvider => _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Serilog 설정
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Log.Information("Application starting...");

                // 서비스 컨테이너 설정
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                Log.Information("Services configured successfully");

                // 메인 윈도우 시작
                var mainWindow = new MainWindow();
                Log.Information("MainWindow created");
                
                mainWindow.Show();
                Log.Information("MainWindow shown");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder => builder.AddSerilog());

            // 핵심 서비스들
            services.AddSingleton<SerialCommunicationService>();
            services.AddSingleton<SimpleQueueService>();
            services.AddSingleton<SimpleHttpService>();
            services.AddSingleton<TimerService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Views - MainWindow는 DI에서 제외하고 직접 생성
            //services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
