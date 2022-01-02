using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace L4d2AddonsMgr.ValueConvertersSpace {

    public class FilterButtonCheckedStateConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as List<MainWindowViewModel.VpkFilter>).Contains(parameter as MainWindowViewModel.VpkFilter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
