using System.Windows.Input;

namespace ReportesDePaqueteria.MVVM.Helpers
{
    public class ShellNavHelper
    {
        public ICommand Command => new Command<string>(async (route) =>
        {
            if (string.IsNullOrWhiteSpace(route)) return;
            await Shell.Current.GoToAsync(route);
        });
    }
}
