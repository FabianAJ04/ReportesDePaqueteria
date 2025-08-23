using System;
using System.Globalization;

namespace ReportesDePaqueteria.Converters
{
    public class CategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string categoria = value?.ToString() ?? "";
            return categoria switch
            {
                "1" => "Entrega",
                "2" => "Devolución",
                "3" => "Reclamación",
                _ => "Otro"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
