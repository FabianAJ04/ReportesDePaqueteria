using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class UserListPage : ContentPage
    {
        public ObservableCollection<UsuarioItem> Usuarios { get; set; }
        public ObservableCollection<string> Roles { get; } =
            new ObservableCollection<string> { "Administrador", "Supervisor", "Operador", "Invitado" };
        public ObservableCollection<string> Estados { get; } =
            new ObservableCollection<string> { "Activo", "Inactivo", "Suspendido" };

        public string SearchText { get; set; }
        public string RolSel { get; set; }
        public string EstadoSel { get; set; }

        public UserListPage()
        {
            InitializeComponent();

            Usuarios = new ObservableCollection<UsuarioItem>
            {
                new UsuarioItem { Id=1, Nombre="Ana Rojas",      Correo="ana.rojas@empresa.com",      Rol="Administrador", Estado="Activo" },
                new UsuarioItem { Id=2, Nombre="Carlos Méndez",  Correo="carlos.mendez@empresa.com",  Rol="Supervisor",    Estado="Activo" },
                new UsuarioItem { Id=3, Nombre="Luis Romero",    Correo="luis.romero@empresa.com",    Rol="Operador",      Estado="Suspendido" },
                new UsuarioItem { Id=4, Nombre="María Pérez",    Correo="maria.perez@empresa.com",    Rol="Operador",      Estado="Inactivo" },
                new UsuarioItem { Id=5, Nombre="Sofía Delgado",  Correo="sofia.delgado@empresa.com",  Rol="Invitado",      Estado="Activo" }
            };

            BindingContext = this;
        }

        private async void OnNuevoClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Nuevo", "Abrir formulario de usuario (pendiente)", "OK");
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is UsuarioItem u)
                await DisplayAlert("Editar", $"Editar usuario: {u.Nombre}", "OK");
        }

        private async void OnDesactivarClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is UsuarioItem u)
                await DisplayAlert("Desactivar", $"Desactivar usuario: {u.Nombre}", "OK");
        }
    }

    public class UsuarioItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
    }
}
