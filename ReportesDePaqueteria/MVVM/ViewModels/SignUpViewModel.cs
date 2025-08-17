using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using ReportesDePaqueteria.MVVM.Models;


namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _email;

        [ObservableProperty]
        private string _username;

        [ObservableProperty]
        private string _password;

        public SignUpViewModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }

        [RelayCommand]
        private async Task SignUp()
        {
            var authResult = await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

            var user = new UserModel
            {
                Id = authResult.User.Uid,
                Name = Name,
                Email = Email,
                Role = 4 // Rol 4: Usuario normal
            };

            var repo = new UserRepository();
            await repo.CreateDocumentAsync(user);

            await Application.Current.MainPage.DisplayAlert("Registro", "¡Usuario registrado correctamente!", "OK");


            Name = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            Username = string.Empty;

            await Shell.Current.GoToAsync("//SignIn");
        }

        [RelayCommand]
        private async Task NavigateSignIn()
        {
            await Shell.Current.GoToAsync("//SignIn");
        }
    }
}
