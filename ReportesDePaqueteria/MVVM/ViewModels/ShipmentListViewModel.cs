using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using System.Collections.ObjectModel;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentListViewModel : ObservableObject
    {
        private readonly IShipmentRepository _repo;
        private readonly IUserRepository _users;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? search;
        [ObservableProperty] private int statusFilter; // 0=todos, 1 Enviado, 2 En tránsito, 3 Entregado, 4 Cancelado, 5 Con incidente
        [ObservableProperty] private bool isAdmin;
        [ObservableProperty] private bool isWorker;
        [ObservableProperty] private string currentUserId = "";

        public ObservableCollection<ShipmentModel> Items { get; } = new();
        public ObservableCollection<ShipmentModel> ViewItems { get; } = new();

        public ShipmentListViewModel(IShipmentRepository repo, IUserRepository users)
        {
            _repo = repo;
            _users = users;
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
                // Obtener usuario actual
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

                // Determinar rol
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
                        // ADMIN ve TODOS los envíos
                        shouldInclude = true;
                    }
                    else if (IsWorker)
                    {
                        // TRABAJADOR ve solo los envíos que le fueron ASIGNADOS
                        shouldInclude = shipment.Worker?.Id == CurrentUserId;
                    }
                    else if (isRegularUser)
                    {
                        // USUARIO NORMAL ve solo los envíos que ÉL CREÓ
                        shouldInclude = shipment.Sender?.Id == CurrentUserId;
                    }

                    if (shouldInclude)
                    {
                        filteredShipments.Add(shipment);
                    }
                }

                // Ordenar por fecha de creación (más recientes primero)
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
            // Solo usuarios normales pueden crear nuevos envíos
            var uid = await SecureStorage.GetAsync("user_id");
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var user = await _users.GetByIdAsync(uid);
                if (user?.Role == 3) // Usuario normal
                {
                    await Shell.Current.GoToAsync(nameof(ShipmentFormPage));
                }
                else
                {
                    await Shell.Current.DisplayAlert("Acceso denegado",
                        "Solo los usuarios pueden crear nuevos envíos.", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task AssignWorkerAsync(ShipmentModel? shipment)
        {
            if (!IsAdmin || shipment == null) return;

            try
            {
                // Obtener lista de trabajadores disponibles
                var allUsers = await _users.GetAllAsync();
                var workers = allUsers.Values.Where(u => u.Role == 2).ToList();

                if (!workers.Any())
                {
                    await Shell.Current.DisplayAlert("Sin trabajadores",
                        "No hay trabajadores disponibles para asignar.", "OK");
                    return;
                }

                // Crear lista de opciones para mostrar al admin
                var workerNames = workers.Select(w => $"{w.Name} ({w.Email})").ToArray();
                var selectedWorker = await Shell.Current.DisplayActionSheet(
                    "Seleccionar trabajador", "Cancelar", null, workerNames);

                if (selectedWorker != "Cancelar" && selectedWorker != null)
                {
                    var workerIndex = Array.IndexOf(workerNames, selectedWorker);
                    if (workerIndex >= 0)
                    {
                        shipment.Worker = workers[workerIndex];
                        shipment.Status = 2; // Cambiar a "En tránsito"

                        await _repo.UpdateAsync(shipment);

                        // Crear notificación para el trabajador
                        await CreateWorkerNotificationAsync(shipment);

                        await Shell.Current.DisplayAlert("Éxito",
                            $"Trabajador {workers[workerIndex].Name} asignado al envío {shipment.Code}", "OK");

                        // Recargar la lista
                        await LoadAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error",
                    $"Error al asignar trabajador: {ex.Message}", "OK");
            }
        }

        private async Task CreateWorkerNotificationAsync(ShipmentModel shipment)
        {
            try
            {
                var notification = new NotificationModel
                {
                    Id = DateTime.UtcNow.Ticks.GetHashCode(), // ID único
                    Title = "Nuevo envío asignado",
                    Message = $"Se te ha asignado el envío {shipment.Code} de {shipment.Origin} a {shipment.Destination}",
                    Priority = 2, // Medium priority
                    IsRead = false,
                    Timestamp = DateTime.UtcNow,
                    Shipment = shipment
                };

                var notificationRepo = new NotificationRepository();
                await notificationRepo.CreateDocumentAsync(notification);

                System.Diagnostics.Debug.WriteLine($"[ShipmentList] Notificación creada para trabajador {shipment.Worker?.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentList] Error al crear notificación: {ex.Message}");
            }
        }
    }
}