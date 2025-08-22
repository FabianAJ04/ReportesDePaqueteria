using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using ReportesDePaqueteria.MVVM.Models;
using System.Text.RegularExpressions;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IUserRepository _userRepository;

        [ObservableProperty] private string name;
        [ObservableProperty] private string email;
        [ObservableProperty] private string username;
        [ObservableProperty] private string password;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string errorMessage;

        public SignUpViewModel(FirebaseAuthClient authClient, IUserRepository userRepository)
        {
            _authClient = authClient;
            _userRepository = userRepository;
        }

        [RelayCommand]
        private async Task SignUp()
        {
            if (IsBusy) return;
            ErrorMessage = string.Empty;

            // Validación
            if (string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Completa nombre, correo y contraseña.";
                await Shell.Current.DisplayAlert("Registro", ErrorMessage, "OK");
                return;
            }
            if (!Regex.IsMatch(Email, @"^\S+@\S+\.\S+$"))
            {
                ErrorMessage = "Correo inválido.";
                await Shell.Current.DisplayAlert("Registro", ErrorMessage, "OK");
                return;
            }
            if (Password.Length < 6)
            {
                ErrorMessage = "La contraseña debe tener al menos 6 caracteres.";
                await Shell.Current.DisplayAlert("Registro", ErrorMessage, "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // 1) Crear en Firebase Auth
                var normalizedEmail = Email.Trim().ToLowerInvariant();
                var displayName = string.IsNullOrWhiteSpace(Username) ? Name : Username;

                var authResult = await _authClient.CreateUserWithEmailAndPasswordAsync(
                    normalizedEmail, Password, displayName);

                var uid = authResult?.User?.Uid;
                if (string.IsNullOrEmpty(uid))
                {
                    ErrorMessage = "No se pudo crear el usuario en Firebase.";
                    await Shell.Current.DisplayAlert("Registro", ErrorMessage, "OK");
                    return;
                }

                // 2) Guardar tokens/sesión para DB autenticada
                var idToken = await authResult.User.GetIdTokenAsync();
                if (!string.IsNullOrWhiteSpace(idToken))
                    await SecureStorage.SetAsync("id_token", idToken);

                await SecureStorage.SetAsync("user_id", uid);

                // 3) Guardar perfil en Realtime DB: Usuarios/{uid}
                var user = new UserModel
                {
                    Id = uid,
                    Name = Name,
                    Email = normalizedEmail,
                    Role = 2
                };

                await _userRepository.CreateAsync(user);

                await Shell.Current.DisplayAlert("Registro", "¡Usuario registrado correctamente!", "OK");

                // 4) Limpiar y navegar
                Name = Email = Password = Username = string.Empty;
                await Shell.Current.GoToAsync("//SignIn");
            }
            catch (FirebaseAuthException fex)
            {
                ErrorMessage = fex.Reason switch
                {
                    AuthErrorReason.EmailExists => "Ese correo ya está registrado.",
                    AuthErrorReason.WeakPassword => "La contraseña es demasiado débil.",
                    AuthErrorReason.InvalidEmailAddress => "El correo no es válido.",
                    _ => "No fue posible completar el registro."
                };
                await Shell.Current.DisplayAlert("Registro", ErrorMessage, "OK");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar: {ex.Message}";
                await Shell.Current.DisplayAlert("Registro", ErrorMessage, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task NavigateSignIn()
        {
            await Shell.Current.GoToAsync("//SignIn");
        }
    }
}
