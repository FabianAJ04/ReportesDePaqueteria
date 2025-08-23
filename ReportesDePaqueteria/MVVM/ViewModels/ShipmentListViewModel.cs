using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentListViewModel : ObservableObject
    {
        private readonly IShipmentRepository _repo;
        private readonly IUserRepository _users;
        private readonly INotificationRepository _notifications;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? search;
        [ObservableProperty] private int statusFilter; // 0=todos, 1 Enviado, 2 En tránsito, 3 Entregado, 4 Cancelado, 5 Con incidente
        [ObservableProperty] private bool isAdmin;
        [ObservableProperty] private bool isWorker;
        [ObservableProperty] private string currentUserId = "";

        public ObservableCollection<ShipmentModel> Items { get; } = new();
        public ObservableCollection<ShipmentModel> ViewItems { get; } = new();

        public ShipmentListViewModel(IShipmentRepository repo, IUserRepository users, INotificationRepository notifications)
        {
            _repo = repo;
            _users = users;
            _notifications = notifications;

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Search) || e.PropertyName == nameof(StatusFilter))
                    ApplyFilter();
            };
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
                    await Shell.Current.DisplayAlert("Error", "Usuario no autenticado", "OK");
                    return;
                }

                CurrentUserId = uid;
                var currentUser = await _users.GetByIdAsync(uid);
                if (currentUser == null)
                {
                    await Shell.Current.DisplayAlert("Error", "No se encontró información del usuario", "OK");
                    return;
                }

                IsAdmin = currentUser.Role == 1;
                IsWorker = currentUser.Role == 2;
                bool isRegularUser = currentUser.Role == 3;

                Items.Clear();
                ViewItems.Clear();

                var allShipments = await _repo.GetAllAsync();
                var filteredShipments = new List<ShipmentModel>();

                foreach (var shipment in allShipments.Values)
                {
                    bool shouldInclude = false;

                    if (IsAdmin)
                    {
                        shouldInclude = true;
                    }
                    else if (IsWorker)
                    {
                        shouldInclude = shipment.Worker?.Id == CurrentUserId;
                    }
                    else if (isRegularUser)
                    {
                        shouldInclude = shipment.Sender?.Id == CurrentUserId;
                    }

                    if (shouldInclude)
                        filteredShipments.Add(shipment);
                }

                foreach (var s in filteredShipments.OrderByDescending(x => x.CreatedDate))
                {
                    Items.Add(s);
                    ViewItems.Add(s);
                }

                System.Diagnostics.Debug.WriteLine($"[ShipmentList] Usuario {uid}, Rol: {currentUser.Role}, Envíos cargados: {filteredShipments.Count}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error al cargar envíos: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"[ShipmentList] Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            ViewItems.Clear();
            var q = (Search ?? string.Empty).Trim().ToLowerInvariant();
            var filtered = Items.AsEnumerable();

            if (StatusFilter != 0)
                filtered = filtered.Where(s => s.Status == StatusFilter);

            if (!string.IsNullOrEmpty(q))
                filtered = filtered.Where(s =>
                    (s.Code ?? "").ToLowerInvariant().Contains(q) ||
                    (s.ReceiverName ?? "").ToLowerInvariant().Contains(q) ||
                    (s.Description ?? "").ToLowerInvariant().Contains(q) ||
                    (s.Origin ?? "").ToLowerInvariant().Contains(q) ||
                    (s.Destination ?? "").ToLowerInvariant().Contains(q));

            foreach (var s in filtered)
                ViewItems.Add(s);
        }

        [RelayCommand]
        private async Task OpenAsync(ShipmentModel? s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Code)) return;
            await Shell.Current.GoToAsync($"{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(s.Code)}");
        }

        [RelayCommand]
        private async Task NewAsync()
        {
            var uid = await SecureStorage.GetAsync("user_id");
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var user = await _users.GetByIdAsync(uid);
                if (user?.Role == 3)
                {
                    await Shell.Current.GoToAsync(nameof(ShipmentFormPage));
                }
                else
                {
                    await Shell.Current.DisplayAlert("Acceso denegado", "Solo los usuarios pueden crear nuevos envíos.", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task AssignWorkerAsync(ShipmentModel? shipment)
        {
            if (!IsAdmin || shipment == null) return;

            try
            {
                var allUsers = await _users.GetAllAsync();
                var workers = allUsers.Values.Where(u => u.Role == 2).ToList();

                if (!workers.Any())
                {
                    await Shell.Current.DisplayAlert("Sin trabajadores", "No hay trabajadores disponibles para asignar.", "OK");
                    return;
                }

                var workerNames = workers.Select(w => $"{w.Name} ({w.Email})").ToArray();
                var selectedWorker = await Shell.Current.DisplayActionSheet("Seleccionar trabajador", "Cancelar", null, workerNames);

                if (selectedWorker != "Cancelar" && selectedWorker != null)
                {
                    var workerIndex = Array.IndexOf(workerNames, selectedWorker);
                    if (workerIndex >= 0)
                    {
                        var worker = workers[workerIndex];

                        shipment.Worker = worker;
                        shipment.Status = 2; // En tránsito
                        await _repo.UpdateAsync(shipment);

                        await CreateWorkerNotificationAsync(shipment, worker);

                        await Shell.Current.DisplayAlert("Éxito",
                            $"Trabajador {worker.Name} asignado al envío {shipment.Code}", "OK");

                        await LoadAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error al asignar trabajador: {ex.Message}", "OK");
            }
        }

        private async Task CreateWorkerNotificationAsync(ShipmentModel shipment, UserModel worker)
        {
            try
            {
                var n = new NotificationModel
                {
                    Type = NotificationType.ShipmentCreated, // Reutilizamos el tipo disponible
                    Title = "Nuevo envío asignado",
                    Message = $"Se te ha asignado el envío {shipment.Code} de {shipment.Origin} a {shipment.Destination}",
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    ShipmentCode = shipment.Code,
                    DeepLink = $"/{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(shipment.Code)}"
                };

                await _notifications.CreateForUserIfSupportedAsync(worker.Id, n);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentList] Error al crear notificación: {ex.Message}");
            }
        }
    }

    public static class NotificationRepoCompatExtensions
    {
        public static async Task CreateForUserIfSupportedAsync(this INotificationRepository repo, string userId, NotificationModel n)
        {
            var mi = repo.GetType().GetMethod("CreateForUserAsync", new[] { typeof(string), typeof(NotificationModel) });
            if (mi != null)
            {
                var task = mi.Invoke(repo, new object[] { userId, n }) as Task;
                if (task != null) await task.ConfigureAwait(false);
            }
            else
            {
                // Fallback: crea para el usuario actual (no el destinatario específico)
                await repo.CreateAsync(n).ConfigureAwait(false);
            }
        }
    }
}
