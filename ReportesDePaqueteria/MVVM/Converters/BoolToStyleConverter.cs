using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ReportesDePaqueteria.Converters
{
    public class BoolToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = parameter?.ToString()?.Split('|');
            var activeStyleKey = parts?[0];
            var inactiveStyleKey = parts?.Length > 1 ? parts[1] : null;

            bool isActive = value is bool b && b;

            if (isActive && Application.Current.Resources.TryGetValue(activeStyleKey, out var active))
                return active;

            if (!isActive && inactiveStyleKey != null && Application.Current.Resources.TryGetValue(inactiveStyleKey, out var inactive))
                return inactive;

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

