using System.Globalization;

namespace ReportesDePaqueteria.Converters
{
    public class BoolFromStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s && !string.IsNullOrWhiteSpace(s);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
