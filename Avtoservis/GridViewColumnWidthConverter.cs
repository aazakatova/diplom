using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Avtoservis
{
    public class GridViewColumnWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualWidth && parameter is string ratioStr)
            {
                // Вычитаем фиксированные ширины других колонок (80 + 200 + 100 = 380)
                double remainingWidth = actualWidth - 480;

                if (double.TryParse(ratioStr, out double ratio) && remainingWidth > 0)
                {
                    return remainingWidth * ratio;
                }
                return remainingWidth > 0 ? remainingWidth : actualWidth * 0.4; // fallback
            }
            return 200; // default width
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
