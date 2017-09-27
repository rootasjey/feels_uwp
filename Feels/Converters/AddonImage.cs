using System;
using System.Collections.Generic;
using Windows.Services.Store;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Feels.Converters {
    public class AddonImage : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var images = (IReadOnlyList<StoreImage>)value;

            if (images == null || images.Count == 0) {
                return new BitmapImage();
            }

            return new BitmapImage(images[0].Uri);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
