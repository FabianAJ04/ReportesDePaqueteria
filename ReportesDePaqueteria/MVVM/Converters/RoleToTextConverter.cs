using System.Globalization;

namespace ReportesDePaqueteria.MVVM.Converters;

public class RoleToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Role: 1 = Admin, 2 = Trabajador, 3 = Usuario (default)
        if (value is int r)
        {
            return r switch
            {
                1 => "Admin",
                2 => "Trabajador",
                3 => "Usuario",
                _ => "Usuario"
            };
        }

        var s = value?.ToString();
        return s switch
        {
            "1" => "Admin",
            "2" => "Trabajador",
            "3" => "Usuario",
            _ => "Usuario"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Admin" => 1,
            "Trabajador" => 2,
            "Usuario" => 3,
            _ => 3 // Default a Usuario
        };
    }
}