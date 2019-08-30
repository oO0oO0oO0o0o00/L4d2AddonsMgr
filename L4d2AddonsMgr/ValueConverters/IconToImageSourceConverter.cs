using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace L4d2AddonsMgr.ValueConvertersSpace {

    // https://stackoverflow.com/questions/2572734/how-do-i-use-standard-windows-warning-error-icons-in-my-wpf-app
    public class IconToImageSourceConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is Icon icon)) {
                Debug.WriteLine("Attempted to convert {0} instead of Icon object in IconToImageSourceConverter", value);
                return null;
            }

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return imageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
