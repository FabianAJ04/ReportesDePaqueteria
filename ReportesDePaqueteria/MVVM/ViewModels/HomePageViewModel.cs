using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Views;
using ReportesDePaqueteria.MVVM.Models;
using Firebase.Database.Streaming;

namespace ReportesDePaqueteria.MVVM.ViewModels;

public partial class HomePageViewModel : ObservableObject
{
    private readonly IUserRepository _users;
    private readonly INotificationRepository _notifications;

    [ObservableProperty] private bool isAdmin = false;
    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private int unreadCount;

    private IDisposable? _sub;

    public HomePageViewModel(IUserRepository users, INotificationRepository notifications)
    {
        _users = users;
        _notifications = notifications;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var uid = await SecureStorage.GetAsync("user_id");
            if (string.IsNullOrWhiteSpace(uid))
            {
                IsAdmin = false;
                UnreadCount = 0;
                return;
            }

            var me = await _users.GetByIdAsync(uid);
            IsAdmin = (me?.Role ?? 3) == 1; // 1=admin, 3=user

            await LoadUnreadAsync();

            await StartListeningAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StartListeningAsync()
    {
        _sub?.Dispose();
        _sub = _notifications.ObserveCurrentUser()
            .Subscribe(HandleNotificationEvent);

        await Task.CompletedTask;
    }

    public void StopListening()
    {
        _sub?.Dispose();
        _sub = null;
    }

    private void HandleNotificationEvent(FirebaseEvent<NotificationModel> e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await LoadUnreadAsync(); } catch { }
        });
    }

    private async Task LoadUnreadAsync()
    {
        try
        {
            var list = await _notifications.GetLatestForCurrentUserAsync(200);
            UnreadCount = list.Count(x => !x.IsRead);
        }
        catch
        {
            UnreadCount = 0;
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
