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
            var vm = (HomePageViewModel)BindingContext;
            await vm.LoadAsync(); 
        }


        private async void OnIncidentesClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(IncidentListPage));

        private async void OnNuevoIncidenteClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(IncidentFormPage));

        private async void OnNotificationsClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(NotificationsPage));

        private async void OnNavHomeClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(HomePage));

        private async void OnNavProfileClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(UserProfilePage));

        private async void OnShipmentsClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShipmentListPage));

        private async void OnNuevoShipmentClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShipmentFormPage));

        private async void OnRecursosClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ResourceAssignPage));

        private async void OnUsuariosClicked(object sender, EventArgs e)
        {
            if (!_vm.IsAdmin)
            {
                await DisplayAlert("Acceso denegado", "Solo administradores pueden acceder.", "OK");
                return;
            }
            await Shell.Current.GoToAsync(nameof(UserListPage));
        }
    }
}
