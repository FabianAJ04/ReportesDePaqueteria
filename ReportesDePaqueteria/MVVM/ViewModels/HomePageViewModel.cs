using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Views; 


namespace ReportesDePaqueteria.MVVM.ViewModels;

public partial class HomePageViewModel : ObservableObject
{
    private readonly IUserRepository _users;

    [ObservableProperty] private bool isAdmin = false;
    [ObservableProperty] private bool isBusy;

    public HomePageViewModel(IUserRepository users)
    {
        _users = users;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (isBusy) return;
        IsBusy = true;

        try
        {
            var uid = await SecureStorage.GetAsync("user_id");
            if (string.IsNullOrWhiteSpace(uid))
            {
                IsAdmin = false;
                return;
            }

            var me = await _users.GetByIdAsync(uid);
            IsAdmin = (me?.Role ?? 3) == 1; // 1=admin, 3=user
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToUsers()
    {
        if (!IsAdmin)
        {
            await Shell.Current.DisplayAlert("Acceso denegado", "Solo administradores pueden acceder.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(UserListPage));
    }
}
