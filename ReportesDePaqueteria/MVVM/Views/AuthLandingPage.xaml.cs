using Microsoft.Maui.Controls;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class AuthLandingPage : ContentPage
    {
        public AuthLandingPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, System.EventArgs e)
    => await Shell.Current.GoToAsync("//SignIn");


        private async void OnSignUpClicked(object sender, System.EventArgs e)
 => await Shell.Current.GoToAsync("//SignUp");

    }
}
