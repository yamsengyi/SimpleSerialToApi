using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Interfaces;
using SimpleSerialToApi.Services;
using SimpleSerialToApi.ViewModels;

namespace SimpleSerialToApi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure logging
            services.AddLogging(configure => configure.AddConsole());
            
            // Register Configuration Service
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Register Serial Communication Service
            services.AddSingleton<ISerialCommunicationService, SerialCommunicationService>();
            
            // Register API Services
            services.AddSingleton<IHttpApiClientService, HttpApiClientService>();
            
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

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}