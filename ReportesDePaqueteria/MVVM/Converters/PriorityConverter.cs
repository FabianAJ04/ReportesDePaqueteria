using System;
using System.Globalization;

namespace ReportesDePaqueteria.MVVM.Converters
{
    public class PriorityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int p)
            {
                return p switch
                {
                    1 => "Baja",
                    2 => "Media",
                    3 => "Alta",
                    4 => "Crítica",
                    _ => "Desconocida"
                };
            }

            var s = value?.ToString();
            return s switch
            {
                "1" => "Baja",
                "2" => "Media",
                "3" => "Alta",
                "4" => "Crítica",
                _ => "Desconocida"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Baja" => 1,
                "Media" => 2,
                "Alta" => 3,
                "Crítica" => 4,
                _ => 0
            };
        }
    }
}
