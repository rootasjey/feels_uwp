using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Feels.Converters {
    public class WeatherIcon : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return null;
            var icon = (string)value;

            var path = "";
            switch (icon) {
                case "clear-day":
                    path = "ms-appx:///Assets/Icons/clair.png";
                    break;
                case "clear-night":
                    path = "ms-appx:///Assets/Icons/moon.png";
                    break;
                case "partly-cloudy-day":
                    path = "ms-appx:///Assets/Icons/partycloudy_day.png";
                    break;
                case "partly-cloudy-night":
                    path = "ms-appx:///Assets/Icons/partycloudy_night.png";
                    break;
                case "cloudy":
                    path = "ms-appx:///Assets/Icons/cloudy.png";
                    break;
                case "rain":
                    path = "ms-appx:///Assets/Icons/cloudrain.png";
                    break;
                case "sleet": // neige fondu
                    path = "ms-appx:///Assets/Icons/sleet.png";
                    break;
                case "snow":
                    path = "ms-appx:///Assets/Icons/snow.png";
                    break;
                case "wind":
                    path = "ms-appx:///Assets/Icons/wind.png";
                    break;
                case "fog":
                    path = "ms-appx:///Assets/Icons/fog.png";
                    break;
                default:
                    path = "ms-appx:///Assets/Icons/clair.png";
                    break;
            }

            return new Uri(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
