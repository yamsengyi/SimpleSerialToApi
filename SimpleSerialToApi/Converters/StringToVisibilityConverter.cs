using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleSerialToApi.Converters
{
    /// <summary>
    /// 문자열이 비어있거나 null이면 Visible, 값이 있으면 Collapsed 반환
    /// (ValueTemplate 플레이스홀더 표시용)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
