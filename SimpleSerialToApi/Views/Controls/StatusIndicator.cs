using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimpleSerialToApi.ViewModels;

namespace SimpleSerialToApi.Views.Controls
{
    public class StatusIndicator : Control
    {
        static StatusIndicator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusIndicator), 
                new FrameworkPropertyMetadata(typeof(StatusIndicator)));
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(ConnectionStatus), typeof(StatusIndicator),
                new PropertyMetadata(ConnectionStatus.Disconnected, OnStatusChanged));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(double), typeof(StatusIndicator),
                new PropertyMetadata(12.0));

        public static readonly DependencyProperty ShowTextProperty =
            DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(StatusIndicator),
                new PropertyMetadata(true));

        public ConnectionStatus Status
        {
            get => (ConnectionStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public bool ShowText
        {
            get => (bool)GetValue(ShowTextProperty);
            set => SetValue(ShowTextProperty, value);
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusIndicator indicator)
            {
                indicator.UpdateStatusBrush();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateStatusBrush();
        }

        private void UpdateStatusBrush()
        {
            var brush = Status switch
            {
                ConnectionStatus.Connected => Brushes.Green,
                ConnectionStatus.Connecting => Brushes.Orange,
                ConnectionStatus.Disconnected => Brushes.Gray,
                ConnectionStatus.Error => Brushes.Red,
                _ => Brushes.Gray
            };

            SetValue(ForegroundProperty, brush);
        }
    }
}