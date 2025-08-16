using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;

namespace ReportesDePaqueteria.MVVVM.ViewModels
{

    public partial class SignInViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private string _email;

        [ObservableProperty]
        private string _password;

        public SignInViewModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }

        [RelayCommand]
        private async Task SignIn()
        {
            try
            {
                var authResult = await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
                var userId = authResult.User.Uid;


                await SecureStorage.SetAsync("user_id", userId);

                // Buscar al usuario en Realtime DB para redirección
                var userRepo = new UserRepository();
                // Verificar existencia del usuario en DB y obtener datos adicionales
                var usuario = await userRepo.GetUserById(userId);

                if (usuario == null)
                {
                    await Shell.Current.DisplayAlert("Error", "Usuario no registrado en la base de datos.", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Error al iniciar sesión: ", "OK");
                return;
            }
        }

            [RelayCommand]
        private async Task NavigateSignUp()
        {
            await Shell.Current.GoToAsync("//SignUp");
        }
    }
}
