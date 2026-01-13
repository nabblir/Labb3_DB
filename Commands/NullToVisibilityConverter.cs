using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Labb3_DB.Commands
    {
    public class NullToVisibilityConverter : IValueConverter
        {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
            if (value == null)
                return Visibility.Collapsed;

            if (value is int intValue && intValue <= 0)
                return Visibility.Collapsed;

            if (value is double doubleValue && doubleValue <= 0)
                return Visibility.Collapsed;

            if (value is float floatValue && floatValue <= 0)
                return Visibility.Collapsed;

            return Visibility.Visible;
            }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
            throw new NotImplementedException();
            }
        }
    }
