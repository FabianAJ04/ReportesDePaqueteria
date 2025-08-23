using System.Globalization;

namespace ReportesDePaqueteria.Converters
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int status)
            {
                return status switch
                {
                    1 => "📤 Enviado",
                    2 => "🚛 En tránsito",
                    3 => "✅ Entregado",
                    4 => "❌ Cancelado",
                    5 => "⚠️ Con incidente",
                    _ => "❓ Desconocido"
                };
            }

            return "❓ Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}