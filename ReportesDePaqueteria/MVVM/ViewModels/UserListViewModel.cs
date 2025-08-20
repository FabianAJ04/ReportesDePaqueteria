using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReportesDePaqueteria.MVVM.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class UserListViewModel : ObservableObject
    {
        private readonly IUserRepository _repo;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string errorMessage;
        [ObservableProperty] private string searchText;

        public ObservableCollection<UserModel> Users { get; } = new();

        public ObservableCollection<UserModel> UsersView { get; } = new();

        public UserListViewModel(IUserRepository repo)
        {
            _repo = repo;

            PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
                ApplyFilter();
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Users.Clear();
                UsersView.Clear();

                var dict = await _repo.GetAllAsync();
                foreach (var u in dict.Values.OrderBy(u => u.Name))
                {
                    Users.Add(u);
                    UsersView.Add(u);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar usuarios: {ex.Message}";
                await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            UsersView.Clear();

            var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
            IEnumerable<UserModel> src = Users;

            if (!string.IsNullOrEmpty(q))
            {
                src = Users.Where(u =>
                    (u.Name ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (u.Email ?? string.Empty).ToLowerInvariant().Contains(q));
            }

            foreach (var u in src)
                UsersView.Add(u);
        }

        [RelayCommand]
        private async Task NuevoAsync()
        {
            await Shell.Current.DisplayAlert("Nuevo usuario", "Abrir formulario (pendiente).", "OK");
        }

        [RelayCommand]
        private async Task EditarAsync(UserModel? user)
        {
            if (user == null) return;

           
            var nuevoRol = user.Role == 1 ? 3 : 1;
            var confirm = await Shell.Current.DisplayAlert(
                "Editar rol",
                $"Cambiar rol de {user.Name} a {(nuevoRol == 1 ? "Admin" : "Usuario")} ?",
                "Sí", "No");

            if (!confirm) return;

            try
            {
                user.Role = nuevoRol;
                await _repo.UpdateAsync(user);
                await Shell.Current.DisplayAlert("Éxito", "Rol actualizado.", "OK");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EliminarAsync(UserModel? user)
        {
            if (user == null) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Eliminar usuario",
                $"¿Eliminar a {user.Name}? Esta acción no se puede deshacer.",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            try
            {
                await _repo.DeleteAsync(user.Id);
                Users.Remove(user);
                UsersView.Remove(user);
                await Shell.Current.DisplayAlert("Eliminado", "Usuario eliminado correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
            }
        }
    }
}
