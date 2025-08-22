using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly HomePageViewModel _vm;

        public HomePage(HomePageViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }

        private async void OnShipmentsClicked(object sender, EventArgs e)
     => await Shell.Current.GoToAsync("//shipments");

        private async void OnIncidentesClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("//incidents");

        // Si agregaste HomePage como un tab (Route="homePage") en el TabBar:
        private async void OnNavHomeClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("//homePage");

        // ===== Páginas secundarias (registradas con Routing.RegisterRoute) =====
        private async void OnNuevoShipmentClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShipmentFormPage));

        private async void OnNuevoIncidenteClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(IncidentFormPage));

        private async void OnNotificationsClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(NotificationsPage));

        private async void OnNavProfileClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(UserProfilePage));

        private async void OnRecursosClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ResourceAssignPage));

        private async void OnUsuariosClicked(object sender, EventArgs e)
        {
            if (!_vm.IsAdmin)
            {
                await DisplayAlert("Acceso denegado", "Solo administradores pueden acceder.", "OK");
                return;
            }
            // Si UserListPage es un tab raíz, usa //home/users; si NO, deja el nameof:
            await Shell.Current.GoToAsync(nameof(UserListPage));
        }
    }
}
