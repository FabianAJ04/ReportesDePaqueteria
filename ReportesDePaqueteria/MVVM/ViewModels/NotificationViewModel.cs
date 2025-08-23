using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using ReportesDePaqueteria.MVVM.Models;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Firebase.Database.Streaming;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class NotificationViewModel : ObservableObject
    {
        private readonly INotificationRepository _repo;
        private readonly List<NotificationModel> _all = new();
        private IDisposable? _subscription;
        private CancellationTokenSource? _searchCts;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string? search;
        [ObservableProperty] private string stateSelected = "Todas";  // Todas | No leídas | Leídas
        [ObservableProperty] private string typeSelected = "Todos";   // Todos | Paquete | Incidencia

        public ObservableCollection<string> StateOptions { get; } = new(new[] { "Todas", "No leídas", "Leídas" });
        public ObservableCollection<string> TypeOptions { get; } = new(new[] { "Todos", "Paquete", "Incidencia" });

        public ObservableCollection<NotificationModel> Notificaciones { get; } = new();

        public NotificationViewModel(INotificationRepository repo)
        {
            _repo = repo;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                _all.Clear();
                Notificaciones.Clear();

                var list = await _repo.GetLatestForCurrentUserAsync(200);
                _all.AddRange(list.OrderByDescending(n => n.Timestamp));

                ApplyFilter();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadAsync();
            IsRefreshing = false;
        }

        public async Task StartListeningAsync()
        {
            await LoadAsync();

            _subscription?.Dispose();
            _subscription = _repo.ObserveCurrentUser()
                .Where(e => e != null && !string.IsNullOrWhiteSpace(e.Key))
                .Subscribe(HandleFirebaseEvent);
        }

        public void StopListening()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void HandleFirebaseEvent(FirebaseEvent<NotificationModel> e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (e.EventType)
                {
                    case FirebaseEventType.Delete:
                        if (int.TryParse(e.Key, out var delId))
                        {
                            _all.RemoveAll(x => x.Id == delId);
                            var item = Notificaciones.FirstOrDefault(x => x.Id == delId);
                            if (item != null) Notificaciones.Remove(item);
                        }
                        break;

                    case FirebaseEventType.InsertOrUpdate:
                    default:
                        var n = e.Object;
                        if (n == null) return;

                        if (n.Id == 0 && int.TryParse(e.Key, out var parsed))
                            n.Id = parsed;

                        var idxAll = _all.FindIndex(x => x.Id == n.Id);
                        if (idxAll >= 0) _all[idxAll] = n;
                        else _all.Insert(0, n);

                        ApplyFilter(); 
                        break;
                }
            });
        }

        [RelayCommand]
        private void ApplyFilters() => ApplyFilter();

        partial void OnStateSelectedChanged(string value) => ApplyFilter();
        partial void OnTypeSelectedChanged(string value) => ApplyFilter();

        partial void OnSearchChanged(string? value)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(180, token); 
                    if (token.IsCancellationRequested) return;
                    MainThread.BeginInvokeOnMainThread(ApplyFilter);
                }
                catch { }
            }, token);
        }

        private void ApplyFilter()
        {
            IEnumerable<NotificationModel> q = _all;

            var text = (Search ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length > 0)
            {
                q = q.Where(n =>
                    (n.Title ?? string.Empty).ToLowerInvariant().Contains(text) ||
                    (n.Message ?? string.Empty).ToLowerInvariant().Contains(text));
            }

            if (StateSelected == "No leídas") q = q.Where(n => !n.IsRead);
            else if (StateSelected == "Leídas") q = q.Where(n => n.IsRead);

            if (TypeSelected == "Paquete") q = q.Where(n => n.Type == NotificationType.ShipmentCreated);
            else if (TypeSelected == "Incidencia") q = q.Where(n => n.Type == NotificationType.IncidentCreated);

            var ordered = q.OrderByDescending(n => n.Timestamp).ToList();

            Notificaciones.Clear();
            foreach (var n in ordered) Notificaciones.Add(n);
        }

        [RelayCommand]
        public async Task OpenAsync(NotificationModel? n)
        {
            if (n is null) return;

            if (!n.IsRead)
                await MarkReadAsync(n);

            if (!string.IsNullOrWhiteSpace(n.DeepLink))
            {
                await Shell.Current.GoToAsync(n.DeepLink);
            }
            else
            {
                if (n.Type == NotificationType.ShipmentCreated && !string.IsNullOrWhiteSpace(n.ShipmentCode))
                    await Shell.Current.GoToAsync($"/ShipmentDetailPage?code={Uri.EscapeDataString(n.ShipmentCode)}");
                else if (n.Type == NotificationType.IncidentCreated && n.IncidentId is int iid)
                    await Shell.Current.GoToAsync($"/IncidentDetailPage?id={iid}");
            }
        }

        [RelayCommand]
        public async Task MarkReadAsync(NotificationModel? n)
        {
            if (n is null || n.IsRead) return;
            try
            {
                n.IsRead = true;
                await _repo.MarkAsReadAsync(n.Id);

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifVM] MarkRead error: {ex}");
            }
        }

        [RelayCommand]
        public async Task DeleteAsync(NotificationModel? n)
        {
            if (n is null) return;

            try
            {
                if (_repo is INotificationRepositoryWithDelete rdel)
                    await rdel.DeleteAsync(n.Id);

                _all.RemoveAll(x => x.Id == n.Id);
                var item = Notificaciones.FirstOrDefault(x => x.Id == n.Id);
                if (item != null) Notificaciones.Remove(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifVM] Delete error: {ex}");
            }
        }

        [RelayCommand]
        public async Task MarkAllReadAsync()
        {
            if (_repo is INotificationRepositoryBulk bulk)
            {
                try { await bulk.MarkAllAsReadAsync(); } catch {  }
            }
            else
            {
                foreach (var n in _all.Where(x => !x.IsRead).ToList())
                {
                    n.IsRead = true;
                    try { await _repo.MarkAsReadAsync(n.Id); } catch {  }
                }
            }
            ApplyFilter();
        }

        [RelayCommand]
        public async Task ClearAllAsync()
        {
            if (_repo is INotificationRepositoryBulk bulk)
            {
                try { await bulk.ClearAllAsync(); } catch {  }
                _all.Clear();
                Notificaciones.Clear();
            }
            else
            {
                _all.Clear();
                Notificaciones.Clear();
                await Task.CompletedTask;
            }
        }
    }

    public interface INotificationRepositoryWithDelete : INotificationRepository
    {
        Task DeleteAsync(int id);
    }
    public interface INotificationRepositoryBulk : INotificationRepository
    {
        Task MarkAllAsReadAsync();
        Task ClearAllAsync();
    }
}
