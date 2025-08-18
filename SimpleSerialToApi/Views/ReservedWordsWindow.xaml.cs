using System.Collections.ObjectModel;
using System.Windows;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Views
{
    /// <summary>
    /// Reserved Words information window
    /// </summary>
    public partial class ReservedWordsWindow : Window
    {
        public class ReservedWordInfo
        {
            public string Word { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string ExampleValue { get; set; } = string.Empty;
        }

        public ObservableCollection<ReservedWordInfo> ReservedWords { get; } = new ObservableCollection<ReservedWordInfo>();

        public ReservedWordsWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadReservedWords();
        }

        private void LoadReservedWords()
        {
            var now = System.DateTime.Now;
            var deviceId = System.Configuration.ConfigurationManager.AppSettings["DeviceId"] ?? "DEVICE_001";

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@yyyyMMddHHmmssfff",
                Description = "년월일시분초밀리초 (밀리초 3자리)",
                ExampleValue = now.ToString("yyyyMMddHHmmssfff")
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@yyyyMMddHHmmss",
                Description = "년월일시분초",
                ExampleValue = now.ToString("yyyyMMddHHmmss")
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@yyyyMMdd",
                Description = "년월일",
                ExampleValue = now.ToString("yyyyMMdd")
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@deviceId",
                Description = "설정된 장치 ID",
                ExampleValue = deviceId
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@timestamp",
                Description = "표준 타임스탬프 형식",
                ExampleValue = now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@unixTime",
                Description = "Unix 타임스탬프 (초)",
                ExampleValue = ((System.DateTimeOffset)now).ToUnixTimeSeconds().ToString()
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "@guid",
                Description = "새로운 GUID 생성",
                ExampleValue = System.Guid.NewGuid().ToString()
            });

            ReservedWords.Add(new ReservedWordInfo
            {
                Word = "{data}",
                Description = "전송할 실제 데이터",
                ExampleValue = "실제 시리얼 데이터"
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
