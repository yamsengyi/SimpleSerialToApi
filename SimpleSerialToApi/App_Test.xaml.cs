using System;
using System.Windows;

namespace SimpleSerialToApi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                
                MessageBox.Show("App is starting!", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                
                var mainWindow = new MainWindow();
                mainWindow.Show();
                
                MessageBox.Show("MainWindow created and shown!", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }
    }
}
