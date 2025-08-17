using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SimpleSerialToApi.ViewModels;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;

namespace SimpleSerialToApi.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus status)
            {
                return status switch
                {
                    ConnectionStatus.Connected => WpfBrushes.Green,
                    ConnectionStatus.Connecting => WpfBrushes.Orange,
                    ConnectionStatus.Disconnected => WpfBrushes.Gray,
                    ConnectionStatus.Error => WpfBrushes.Red,
                    _ => WpfBrushes.Gray
                };
            }
            return WpfBrushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ApplicationStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ApplicationState state)
            {
                return state switch
                {
                    ApplicationState.Running => WpfBrushes.Green,
                    ApplicationState.Starting => WpfBrushes.Orange,
                    ApplicationState.Stopping => WpfBrushes.Orange,
                    ApplicationState.Stopped => WpfBrushes.Gray,
                    ApplicationState.Error => WpfBrushes.Red,
                    _ => WpfBrushes.Gray
                };
            }
            return WpfBrushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToColorConverter : IValueConverter
    {
        public WpfBrush TrueColor { get; set; } = WpfBrushes.Green;
        public WpfBrush FalseColor { get; set; } = WpfBrushes.Red;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueColor : FalseColor;
            }
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string level)
            {
                return level.ToUpper() switch
                {
                    "ERROR" => WpfBrushes.Red,
                    "WARN" => WpfBrushes.Orange,
                    "WARNING" => WpfBrushes.Orange,
                    "INFO" => WpfBrushes.Blue,
                    "DEBUG" => WpfBrushes.Gray,
                    "TRACE" => WpfBrushes.LightGray,
                    _ => WpfBrushes.Black
                };
            }
            return WpfBrushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
