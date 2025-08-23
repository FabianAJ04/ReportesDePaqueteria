using System.Globalization;

namespace ReportesDePaqueteria.Converters
{
    public class CategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int c)
            {
                return c switch
                {
                    1 => "Paquete",
                    2 => "Entrega",
                    3 => "Pago",
                    4 => "Otro",
                    _ => "Desconocida"
                };
            }

            return "Desconocida";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Paquete" => 1,
                "Entrega" => 2,
                "Pago" => 3,
                "Otro" => 4,
                _ => 0
            };
        }
    }
}
