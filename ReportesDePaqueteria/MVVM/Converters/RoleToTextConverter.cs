using System.Globalization;

namespace ReportesDePaqueteria.MVVM.Converters;

public class RoleToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Role: 1 = Admin, 3 = User (default)
        if (value is int r) return r == 1 ? "Admin" : "Usuario";
        var s = value?.ToString();
        return s == "1" ? "Admin" : "Usuario";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == "Admin" ? 1 : 3;
}
