using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Feels.Converters {
    public class LocalizedDay : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var day = (DayOfWeek)value;
            return DateTimeFormatInfo.CurrentInfo.GetDayName(day);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
