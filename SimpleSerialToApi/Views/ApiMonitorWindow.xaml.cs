using System.Windows;

namespace SimpleSerialToApi.Views
{
    /// <summary>
    /// API Communication Monitor Window
    /// </summary>
    public partial class ApiMonitorWindow : Window
    {
        public ApiMonitorWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor with DataContext
        /// </summary>
        /// <param name="dataContext">ViewModel to bind</param>
        public ApiMonitorWindow(object dataContext) : this()
        {
            DataContext = dataContext;
        }
    }
}
