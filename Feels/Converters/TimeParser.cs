using System;
using Windows.UI.Xaml.Data;

namespace Feels.Converters {
    public class TimeParser : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return null;
            var time = (DateTimeOffset)value;

            return string.Format("{0}:00", time.LocalDateTime.Hour);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return value;
        }
    }
}
