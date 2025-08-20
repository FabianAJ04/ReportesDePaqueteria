using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views;

public partial class UserProfilePage : ContentPage
{
    private readonly UserProfileViewModel _vm;

    public UserProfilePage(UserProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadAsync(); 
    }
    private async void OnChangePasswordClicked(object sender, EventArgs e)
    => await Shell.Current.GoToAsync(nameof(ChangePasswordPage));
}

