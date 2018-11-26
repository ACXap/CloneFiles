using System;
using System.Globalization;
using System.Windows.Data;

namespace CloneFile
{
    class ConvertSizeToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var size = (long)value;
                if (size == 0)
                {
                    return "Пустой";
                }
                else if (size != 0 && size < 1024)
                {
                    return size + " Б";
                }
                else if (size > 1000 && size < 1048576)
                {
                    return (size / 1024) + " Кб";
                }
                else
                {
                    return (size / 1048576) + " Мб";
                }
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
