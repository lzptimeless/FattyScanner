using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FattyScanner.Converters
{
    public class PercentageFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float || value is double || value is decimal)
            {
                var num = Math.Round((double)value, 2) * 100;
                return $"{num}%";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var percentageStr = value as string;
            if (percentageStr != null)
            {
                var index = percentageStr.IndexOf("%");
                if (index >= 1)
                {
                    var numStr = percentageStr.Substring(0, index);
                    if (double.TryParse(numStr, out double num))
                    {
                        return num / 100;
                    }
                }
            }

            return 0;
        }
    }
}
