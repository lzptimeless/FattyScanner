using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FattyScanner.Converters
{
    public class FileSysDisplayWidthConverter : IValueConverter
    {
        public double TotalDisplayWidth { get; set; } = 1024;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double num)
            {
                var displayWidth = Math.Round(num * TotalDisplayWidth, 2, MidpointRounding.ToZero);
                return displayWidth;
            }
            else
            {
                return 1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double num)
            {
                return num / TotalDisplayWidth;
            }
            else
            {
                return 0;
            }
        }
    }
}
