using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using ReportesDePaqueteria.MVVM.Models;
using System.Data;
using System.Xml.Linq;

namespace ReportesDePaqueteria.MVVM.ViewModels;

public partial class UserProfileViewModel : ObservableObject
{
    private readonly IUserRepository _users;
    private readonly FirebaseAuthClient _auth;

    [ObservableProperty] private string userId;
    [ObservableProperty] private string name;
    [ObservableProperty] private string email;
    [ObservableProperty] private int role;             // 1=Admin, 3=Usuario
    [ObservableProperty] private bool isAdmin;

    [ObservableProperty] private bool notifyEmail;
    [ObservableProperty] private bool notifyPush;
    [ObservableProperty] private bool darkTheme;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage;

    public UserProfileViewModel(IUserRepository users, FirebaseAuthClient auth)
    {
        _users = users;
        _auth = auth;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var uid = await SecureStorage.GetAsync("user_id");
            if (string.IsNullOrWhiteSpace(uid))
            {
                await Shell.Current.DisplayAlert("Sesión", "No hay sesión activa.", "OK");
                await Shell.Current.GoToAsync("//SignIn");
                return;
            }

            UserId = uid;

            var me = await _users.GetByIdAsync(uid);
            if (me is null)
            {
                await Shell.Current.DisplayAlert("Perfil", "No se encontraron datos del usuario.", "OK");
                return;
            }

            Name = me.Name;
            Email = me.Email;
            Role = me.Role;
            IsAdmin = me.Role == 1; // 1=Admin, 3=User

            NotifyEmail = Preferences.Default.Get("pref_notify_email", true);
            NotifyPush = Preferences.Default.Get("pref_notify_push", true);
            DarkTheme = Preferences.Default.Get("pref_dark_theme", false);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar perfil: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var u = new UserModel
            {
                Id = UserId,
                Name = Name?.Trim() ?? string.Empty,
                Email = Email?.Trim().ToLowerInvariant() ?? string.Empty,
                Role = Role // 1 o 3
            };

            await _users.UpdateAsync(u);

            Preferences.Default.Set("pref_notify_email", NotifyEmail);
            Preferences.Default.Set("pref_notify_push", NotifyPush);
            Preferences.Default.Set("pref_dark_theme", DarkTheme);

            await Shell.Current.DisplayAlert("Perfil", "Cambios guardados.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                await Shell.Current.DisplayAlert("Contraseña", "No hay correo para enviar el reset.", "OK");
                return;
            }

            await _auth.ResetEmailPasswordAsync(Email.Trim());
            await Shell.Current.DisplayAlert("Contraseña", "Te enviamos un correo para restablecer tu contraseña.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"No se pudo enviar el correo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        await Shell.Current.DisplayAlert("Foto", "Cambiar foto: pendiente de implementar.", "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            // 1) Cerrar sesión en el cliente (sin async)
            _auth.SignOut();

            // 2) Limpiar credenciales locales
            await SecureStorage.SetAsync("id_token", string.Empty);
            await SecureStorage.SetAsync("user_id", string.Empty);
            // Preferences.Default.Remove("pref_notify_email");
            // Preferences.Default.Remove("pref_notify_push");
            // Preferences.Default.Remove("pref_dark_theme");

            // 3) Ir al login
            await Shell.Current.GoToAsync("//SignIn");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"No se pudo cerrar sesión: {ex.Message}", "OK");
        }
    }
}
