using ReportesDePaqueteria.MVVM.ViewModels;
using Microsoft.Maui.Storage;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ShipmentListPage : ContentPage
    {
        private readonly ShipmentListViewModel _vm;

        public ShipmentListPage(ShipmentListViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Cargar datos
            await _vm.LoadAsync();

            // Actualizar UI según el rol
            await UpdateUIForRole();
        }

        private async Task UpdateUIForRole()
        {
            try
            {
                // Encontrar los elementos por nombre
                var roleHeader = this.FindByName<Label>("RoleHeader");
                var roleDescription = this.FindByName<Label>("RoleDescription");

                if (roleHeader == null || roleDescription == null)
                    return;

                if (_vm.IsAdmin)
                {
                    roleHeader.Text = "👑 Panel de Administrador";
                    roleDescription.Text = "Puedes ver todos los envíos y asignar trabajadores";
                    this.Title = "Gestión de Envíos - Admin";
                }
                else if (_vm.IsWorker)
                {
                    roleHeader.Text = "👷 Panel de Trabajador";
                    roleDescription.Text = "Envíos asignados a ti para gestionar";
                    this.Title = "Mis Envíos Asignados";
                }
                else
                {
                    roleHeader.Text = "📦 Mis Envíos";
                    roleDescription.Text = "Envíos que has creado y su estado actual";
                    this.Title = "Mis Envíos";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentListPage] Error updating UI: {ex.Message}");
            }
        }
    }
}