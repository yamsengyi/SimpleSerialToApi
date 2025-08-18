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
            // 비모달 창에서는 DialogResult를 설정할 수 없으므로 바로 닫기
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
