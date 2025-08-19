using Microsoft.Maui.Controls;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void OnIncidentesClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(IncidentListPage));

        private async void OnNuevoIncidenteClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(IncidentFormPage));

        private async void OnNotificationsClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(NotificationsPage));

        private async void OnNavHomeClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync($"//{nameof(HomePage)}");

        private async void OnNavProfileClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(UserProfilePage));

        private async void OnShipmentsClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShipmentListPage));
        private async void OnNuevoShipmentClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShipmentFormPage));
        private async void OnRecursosClicked(object sender, System.EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ResourceAssignPage));


    }
}
