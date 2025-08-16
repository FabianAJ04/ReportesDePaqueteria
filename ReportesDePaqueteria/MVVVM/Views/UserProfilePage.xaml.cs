using Microsoft.Maui.Controls;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class UserProfilePage : ContentPage
    {
        public UserProfilePage()
        {
            InitializeComponent();
           
        }

        private async void OnChangePhotoClicked(object sender, System.EventArgs e)
            => await DisplayAlert("Foto", ".", "OK");

        private async void OnChangePasswordClicked(object sender, System.EventArgs e)
            => await DisplayAlert("Contrase�a", "", "OK");

        private async void OnSaveClicked(object sender, System.EventArgs e)
            => await DisplayAlert("Perfil", "", "OK");

        private async void OnLogoutClicked(object sender, System.EventArgs e)
            => await DisplayAlert("Sesi�n", "", "OK");
    }

}
