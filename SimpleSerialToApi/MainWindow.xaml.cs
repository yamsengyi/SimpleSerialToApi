using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SimpleSerialToApi.ViewModels;

namespace SimpleSerialToApi
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Simple DataContext setup
                var app = (App)Application.Current;
                if (app?.ServiceProvider != null)
                {
                    DataContext = app.ServiceProvider.GetRequiredService<MainViewModel>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MainWindow initialization failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}