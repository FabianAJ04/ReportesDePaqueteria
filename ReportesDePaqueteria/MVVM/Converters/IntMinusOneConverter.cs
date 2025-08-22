using System.Globalization;

namespace ReportesDePaqueteria.Converters
{
    public class IntMinusOneConverter : IValueConverter
    {
        public static IntMinusOneConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? Math.Max(0, i - 1) : 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? i + 1 : 1;
    }
}
