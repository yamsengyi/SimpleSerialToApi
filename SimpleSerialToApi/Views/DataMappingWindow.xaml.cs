using System.Windows;
using SimpleSerialToApi.ViewModels;

namespace SimpleSerialToApi.Views
{
    /// <summary>
    /// Data Mapping and API Configuration Window
    /// </summary>
    public partial class DataMappingWindow : Window
    {
        public DataMappingWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor with DataContext
        /// </summary>
        /// <param name="dataContext">ViewModel to bind</param>
        public DataMappingWindow(object dataContext) : this()
        {
            DataContext = dataContext;
            
            // ViewModel 이벤트 구독
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.DataMappingWindowCloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, bool saved)
        {
            DialogResult = saved;
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // 이벤트 구독 해제
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.DataMappingWindowCloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}
