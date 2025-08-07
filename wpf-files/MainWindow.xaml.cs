using System.Windows;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(ILogger<MainWindow> logger)
        {
            _logger = logger;
            InitializeComponent();
            
            _logger.LogInformation("MainWindow initialized - Step 01 project setup complete");
        }
    }
}