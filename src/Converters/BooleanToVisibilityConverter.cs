using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FattyScanner.Converters
{
    internal class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = "inverse".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);
            if (value is bool)
            {
                if ((bool)value)
                {
                    return inverse ? Visibility.Collapsed : Visibility.Visible;
                }
            }

            return inverse ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = "inverse".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);
            if (value is Visibility)
            {
                if (((Visibility)value) == Visibility.Visible)
                {
                    return !inverse;
                }
            }

            return inverse;
        }
    }
}
