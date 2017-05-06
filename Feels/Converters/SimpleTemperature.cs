using System;
using Windows.UI.Xaml.Data;

namespace Feels.Converters {
    public class SimpleTemperature : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return null;

            var temperature = (float)value;
            var simpleTemp = (int)temperature;
            return string.Format("{0}°", simpleTemp);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
