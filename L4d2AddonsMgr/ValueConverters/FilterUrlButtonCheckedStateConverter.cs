using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace L4d2AddonsMgr.ValueConvertersSpace {

    public class FilterUrlButtonCheckedStateConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as List<AddonsCollection.VpkFilter>)
            .Contains(AddonsCollection.KnownSourceUrls.RegisteredFilters[parameter as string].Filter);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
