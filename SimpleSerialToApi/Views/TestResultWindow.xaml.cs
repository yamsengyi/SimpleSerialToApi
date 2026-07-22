using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Media = System.Windows.Media;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Views
{
    /// <summary>
    /// Mapping test result display window (resolved values only)
    /// </summary>
    public partial class TestResultWindow : Window
    {
        public ObservableCollection<TestResultItem> Results { get; } = new();

        public TestResultWindow()
        {
            InitializeComponent();
            ResultsContainer.ItemsSource = Results;
        }

        public TestResultWindow(string testInput, List<TestResultDisplay> results) : this()
        {
            TestInputInfo.Text = $"Test Input: \"{testInput}\" | {results.Count} scenario(s) matched";

            foreach (var result in results)
            {
                var item = new TestResultItem
                {
                    Name = result.ScenarioName,
                    TypeText = result.IsApi ? "API" : "SERIAL",
                    TypeColor = result.IsApi
                        ? new Media.SolidColorBrush(Media.Color.FromRgb(33, 150, 243))
                        : new Media.SolidColorBrush(Media.Color.FromRgb(76, 175, 80)),
                    DataText = result.ResolvedData,
                    DataBg = result.IsApi
                        ? new Media.SolidColorBrush(Media.Color.FromRgb(232, 240, 254))
                        : new Media.SolidColorBrush(Media.Color.FromRgb(232, 245, 233)),
                    DataBorder = result.IsApi
                        ? new Media.SolidColorBrush(Media.Color.FromRgb(144, 202, 249))
                        : new Media.SolidColorBrush(Media.Color.FromRgb(165, 214, 167))
                };

                Results.Add(item);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class TestResultItem
    {
        public string Name { get; set; } = string.Empty;
        public string TypeText { get; set; } = string.Empty;
        public Media.Brush TypeColor { get; set; } = Media.Brushes.Gray;
        public string DataText { get; set; } = string.Empty;
        public Media.Brush DataBg { get; set; } = Media.Brushes.Transparent;
        public Media.Brush DataBorder { get; set; } = Media.Brushes.Transparent;
    }
}
