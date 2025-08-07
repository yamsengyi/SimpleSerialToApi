using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using SimpleSerialToApi.ViewModels;

namespace SimpleSerialToApi.Views.Controls
{
    public class LogViewer : Control
    {
        static LogViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LogViewer),
                new FrameworkPropertyMetadata(typeof(LogViewer)));
        }

        public static readonly DependencyProperty LogEntriesProperty =
            DependencyProperty.Register(nameof(LogEntries), typeof(ObservableCollection<LogEntry>), typeof(LogViewer),
                new PropertyMetadata(null, OnLogEntriesChanged));

        public static readonly DependencyProperty MaxEntriesProperty =
            DependencyProperty.Register(nameof(MaxEntries), typeof(int), typeof(LogViewer),
                new PropertyMetadata(1000));

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register(nameof(AutoScroll), typeof(bool), typeof(LogViewer),
                new PropertyMetadata(true));

        public ObservableCollection<LogEntry>? LogEntries
        {
            get => (ObservableCollection<LogEntry>?)GetValue(LogEntriesProperty);
            set => SetValue(LogEntriesProperty, value);
        }

        public int MaxEntries
        {
            get => (int)GetValue(MaxEntriesProperty);
            set => SetValue(MaxEntriesProperty, value);
        }

        public bool AutoScroll
        {
            get => (bool)GetValue(AutoScrollProperty);
            set => SetValue(AutoScrollProperty, value);
        }

        private ListBox? _listBox;
        private ScrollViewer? _scrollViewer;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _listBox = GetTemplateChild("PART_LogList") as ListBox;
            _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            
            if (LogEntries != null && LogEntries.Any())
            {
                ScrollToBottom();
            }
        }

        private static void OnLogEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogViewer logViewer)
            {
                if (e.OldValue is ObservableCollection<LogEntry> oldCollection)
                {
                    oldCollection.CollectionChanged -= logViewer.OnLogEntriesCollectionChanged;
                }

                if (e.NewValue is ObservableCollection<LogEntry> newCollection)
                {
                    newCollection.CollectionChanged += logViewer.OnLogEntriesCollectionChanged;
                }
            }
        }

        private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (AutoScroll && e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke(() => ScrollToBottom(), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void ScrollToBottom()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollToBottom();
            }
            else if (_listBox != null && LogEntries?.Any() == true)
            {
                _listBox.ScrollIntoView(LogEntries.First());
            }
        }
    }
}