using System;
using System.Globalization;
using Xamarin.Forms;

namespace XamarinVideoPlayer.Converters
{
    public class MillisecondsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeSpan.FromSeconds((double)value).ToString(@"hh\:mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeSpan.Parse(value.ToString()).TotalMilliseconds;
        }
    }
}
