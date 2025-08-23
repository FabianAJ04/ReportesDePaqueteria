using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentFormViewModel : ObservableObject
    {
        private readonly IShipmentRepository _shipments;
        private readonly IUserRepository _users;

        [ObservableProperty] private string origin = "";
        [ObservableProperty] private string destination = "";
        [ObservableProperty] private string? receiverName;
        [ObservableProperty] private string? description;
        [ObservableProperty] private bool isBusy;

        public ShipmentFormViewModel(IShipmentRepository shipments, IUserRepository users)
        {
            _shipments = shipments;
            _users = users;
        }
        public ShipmentModel Shipment { get; set; } = new ShipmentModel();

        [RelayCommand]
        private async Task CreateAsync()
        {
            if (IsBusy) return;

            // Validaciones
            if (string.IsNullOrWhiteSpace(ReceiverName))
            {
                await Shell.Current.DisplayAlert("Validación", "Ingresa el nombre del destinatario.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Origin))
            {
                await Shell.Current.DisplayAlert("Validación", "Ingresa el lugar de origen.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Destination))
            {
                await Shell.Current.DisplayAlert("Validación", "Ingresa el lugar de destino.", "OK");
                return;
            }

            IsBusy = true;

            try
            {
                // Verificar que el usuario actual sea un usuario normal (rol 3)
                var uid = await SecureStorage.GetAsync("user_id");
                if (string.IsNullOrWhiteSpace(uid))
                {
                    await Shell.Current.DisplayAlert("Error", "Usuario no autenticado.", "OK");
                    return;
                }

                var sender = await _users.GetByIdAsync(uid);
                if (sender == null)
                {
                    await Shell.Current.DisplayAlert("Error", "No se encontró información del usuario.", "OK");
                    return;
                }

                if (sender.Role != 3) // Solo usuarios normales pueden crear envíos
                {
                    await Shell.Current.DisplayAlert("Acceso denegado",
                        "Solo los usuarios pueden crear nuevos envíos.", "OK");
                    return;
                }

                // Generar código único de seguimiento
                string code;
                int attempts = 0;
                do
                {
                    code = TrackingGenerator.NewCode();
                    attempts++;
                    if (attempts > 12)
                        throw new Exception("No se pudo generar un código único.");
                }
                while ((await _shipments.GetByCodeAsync(code)) != null);

                // Crear el envío
                var shipment = new ShipmentModel
                {
                    Code = code,
                    Sender = sender,
                    Worker = null, // No asignado inicialmente
                    ReceiverName = ReceiverName?.Trim() ?? "",
                    Status = 1, // Estado: Enviado
                    CreatedDate = DateTime.UtcNow,
                    Description = Description?.Trim() ?? "",
                    Incident = null,
                    Origin = Origin?.Trim() ?? "",
                    Destination = Destination?.Trim() ?? ""
                };

                // Guardar el envío en la base de datos
                await _shipments.CreateAsync(shipment);

                // Crear notificación para TODOS los administradores
                await CreateAdminNotificationAsync(shipment);

                // Limpiar el formulario
                Clean();

                await Shell.Current.DisplayAlert("Éxito",
                    $"Envío creado exitosamente.\nCódigo de seguimiento: {shipment.Code}", "OK");

                // Navegar a los detalles del envío
                await Shell.Current.GoToAsync($"{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(shipment.Code)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error",
                    $"No se pudo crear el envío: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"[ShipmentForm] Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Clean()
        {
            Origin = "";
            Destination = "";
            ReceiverName = null;
            Description = null;
        }
        private async Task CreateAdminNotificationAsync(ShipmentModel shipment)
        {
            try
            {
                // Obtener todos los administradores
                var allUsers = await _users.GetAllAsync();
                var admins = allUsers.Values.Where(u => u.Role == 1).ToList();

                if (!admins.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[ShipmentForm] No se encontraron administradores para notificar");
                    return;
                }

                // Crear notificación para cada administrador
                var notificationRepo = new NotificationRepository();

                foreach (var admin in admins)
                {
                    var notification = new NotificationModel
                    {
                        Id = DateTime.UtcNow.Ticks.GetHashCode() + admin.GetHashCode(), // ID único por admin
                        Title = "Nuevo envío creado",
                        Message = $"Nuevo envío {shipment.Code} de {shipment.Sender?.Name ?? "Usuario"} " +
                                $"desde {shipment.Origin} hacia {shipment.Destination}",
                        Priority = 2, // Medium priority
                        IsRead = false,
                        Timestamp = DateTime.UtcNow,
                        Shipment = shipment
                    };

                    await notificationRepo.CreateDocumentAsync(notification);
                    System.Diagnostics.Debug.WriteLine($"[ShipmentForm] Notificación enviada al admin {admin.Name}");
                }

                System.Diagnostics.Debug.WriteLine($"[ShipmentForm] {admins.Count} notificaciones creadas para el envío {shipment.Code}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentForm] Error al crear notificaciones: {ex.Message}");
                // No interrumpir el flujo principal por errores de notificaciones
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            // Limpiar campos y volver atrás
            Origin = "";
            Destination = "";
            ReceiverName = "";
            Description = "";

            await Shell.Current.GoToAsync("..");
        }
    }
}