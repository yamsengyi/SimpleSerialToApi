using System.Windows;
using System.Windows.Controls;

namespace SimpleSerialToApi.Views.Controls
{
    public class ProgressRing : System.Windows.Controls.Control
    {
        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressRing),
                new FrameworkPropertyMetadata(typeof(ProgressRing)));
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ProgressRing),
                new PropertyMetadata(false));

        public static readonly DependencyProperty RingSizeProperty =
            DependencyProperty.Register(nameof(RingSize), typeof(double), typeof(ProgressRing),
                new PropertyMetadata(20.0));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public double RingSize
        {
            get => (double)GetValue(RingSizeProperty);
            set => SetValue(RingSizeProperty, value);
        }
    }
}
