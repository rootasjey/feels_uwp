using System;
using Windows.UI.Xaml.Data;

namespace Feels.Converters {
    public class LocationSelectedIcon : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var selected = (bool)value;
            if (selected) return "\uE008";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
