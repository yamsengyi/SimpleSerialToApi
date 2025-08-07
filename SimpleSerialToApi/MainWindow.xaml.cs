using System.Windows;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.ViewModels;

namespace SimpleSerialToApi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly MainViewModel _viewModel;

        public MainWindow(ILogger<MainWindow> logger, MainViewModel viewModel)
        {
            _logger = logger;
            _viewModel = viewModel;
            
            InitializeComponent();
            DataContext = _viewModel;
            
            _logger.LogInformation("MainWindow initialized with WPF UI - Step 07 complete");
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}