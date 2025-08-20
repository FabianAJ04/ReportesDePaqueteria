using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class SignInViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IUserRepository _userRepository;

        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string errorMessage;

        public SignInViewModel(FirebaseAuthClient authClient, IUserRepository userRepository)
        {
            _authClient = authClient;
            _userRepository = userRepository;
        }

        [RelayCommand]
        private async Task SignIn()
        {
            if (IsBusy) return;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Ingresa correo y contraseña.";
                return;
            }
            if (!Regex.IsMatch(Email, @"^\S+@\S+\.\S+$"))
            {
                ErrorMessage = "Correo inválido.";
                return;
            }

            try
            {
                IsBusy = true;

                // 1) Autenticar
                var authResult = await _authClient.SignInWithEmailAndPasswordAsync(Email.Trim(), Password);
                var userId = authResult?.User?.Uid;
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "No se pudo obtener el usuario.";
                    return;
                }

                // 2) Persistir datos mínimos de sesión
                await SecureStorage.SetAsync("user_id", userId);
                var idToken = await authResult.User.GetIdTokenAsync();
                if (!string.IsNullOrWhiteSpace(idToken))
                    await SecureStorage.SetAsync("id_token", idToken);

                // 3) Verificar existencia en tu DB (Realtime DB)
                var usuario = await _userRepository.GetByIdAsync(userId);
                if (usuario == null)
                {
                    await Shell.Current.DisplayAlert("Error", "Usuario no registrado en la base de datos.", "OK");
                    return;
                }

                // 4) Navegar a Home
                await Shell.Current.GoToAsync("//HomePage");
            }
            catch (FirebaseAuthException fex)
            {
                ErrorMessage = fex.Reason switch
                {
                    AuthErrorReason.WrongPassword => "Contraseña incorrecta.",
                    AuthErrorReason.UnknownEmailAddress => "El correo no está registrado.",
                    AuthErrorReason.UserDisabled => "La cuenta está deshabilitada.",
                    _ => "No fue posible iniciar sesión."
                };
                await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
            }
            catch
            {
                ErrorMessage = "Error inesperado al iniciar sesión.";
                await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task NavigateSignUp()
        {
            await Shell.Current.GoToAsync("//SignUp");
        }
    }
}
