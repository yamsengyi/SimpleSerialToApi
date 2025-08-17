using System.Windows;

namespace SimpleSerialToApi.Views
{
    /// <summary>
    /// Serial Communication Monitor Window
    /// </summary>
    public partial class SerialMonitorWindow : Window
    {
        public SerialMonitorWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor with DataContext
        /// </summary>
        /// <param name="dataContext">ViewModel to bind</param>
        public SerialMonitorWindow(object dataContext) : this()
        {
            DataContext = dataContext;
        }
    }
}
