using System.Windows;

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
        }
    }
}
