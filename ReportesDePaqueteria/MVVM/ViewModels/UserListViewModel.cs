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

        // Propiedades para el modal de edición
        [ObservableProperty] private bool isEditModalVisible;
        [ObservableProperty] private UserModel? selectedUser;
        [ObservableProperty] private string editName = "";
        [ObservableProperty] private string editEmail = "";
        [ObservableProperty] private int selectedRoleIndex = 0; // Para el Picker (0-based)

        public ObservableCollection<UserModel> Users { get; } = new();
        public ObservableCollection<UserModel> UsersView { get; } = new();

        // Opciones para el Picker de roles
        public ObservableCollection<string> RoleOptions { get; } = new()
        {
            "Admin",
            "Trabajador",
            "Usuario"
        };

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

            SelectedUser = user;
            EditName = user.Name ?? "";
            EditEmail = user.Email ?? "";

            // Convertir el rol (1, 2, 3) a índice del picker (0, 1, 2)
            SelectedRoleIndex = user.Role switch
            {
                1 => 0, // Admin
                2 => 1, // Trabajador
                3 => 2, // Usuario
                _ => 2  // Default a Usuario
            };

            IsEditModalVisible = true;
        }

        [RelayCommand]
        private async Task GuardarCambiosAsync()
        {
            if (SelectedUser == null) return;
            if (string.IsNullOrWhiteSpace(EditName))
            {
                await Shell.Current.DisplayAlert("Validación", "El nombre es requerido.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // Convertir índice del picker a valor de rol
                int newRole = SelectedRoleIndex switch
                {
                    0 => 1, // Admin
                    1 => 2, // Trabajador
                    2 => 3, // Usuario
                    _ => 3  // Default a Usuario
                };

                // Actualizar las propiedades del usuario seleccionado
                SelectedUser.Name = EditName.Trim();
                SelectedUser.Email = EditEmail.Trim().ToLowerInvariant();
                SelectedUser.Role = newRole;

                // Guardar en la base de datos
                await _repo.UpdateAsync(SelectedUser);

                // Actualizar las colecciones para que la vista se actualice
                UpdateUserInCollections(SelectedUser);

                IsEditModalVisible = false;
                await Shell.Current.DisplayAlert("Éxito", "Usuario actualizado correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateUserInCollections(UserModel updatedUser)
        {
            // Buscar y actualizar en la colección Users
            var userInUsers = Users.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (userInUsers != null)
            {
                var index = Users.IndexOf(userInUsers);
                Users[index] = updatedUser;
            }

            // Buscar y actualizar en la colección UsersView
            var userInView = UsersView.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (userInView != null)
            {
                var index = UsersView.IndexOf(userInView);
                UsersView[index] = updatedUser;
            }
        }

        [RelayCommand]
        private void CancelarEdicion()
        {
            IsEditModalVisible = false;
            SelectedUser = null;
            EditName = "";
            EditEmail = "";
            SelectedRoleIndex = 0;
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

        // Helper para obtener el texto del rol
        public string GetRoleText(int role) => role switch
        {
            1 => "Admin",
            2 => "Trabajador",
            3 => "Usuario",
            _ => "Usuario"
        };
    }
}