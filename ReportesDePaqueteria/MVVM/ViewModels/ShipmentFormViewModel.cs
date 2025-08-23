using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentFormViewModel : ObservableObject
    {
        private readonly IShipmentRepository _shipments;
        private readonly IUserRepository _users;
        private readonly INotificationRepository _notifications;

        [ObservableProperty] private string origin = "";
        [ObservableProperty] private string destination = "";
        [ObservableProperty] private string? receiverName;
        [ObservableProperty] private string? description;
        [ObservableProperty] private bool isBusy;

        public ShipmentFormViewModel(IShipmentRepository shipments,
                                     IUserRepository users,
                                     INotificationRepository notifications)
        {
            _shipments = shipments;
            _users = users;
            _notifications = notifications;
        }

        public ShipmentModel Shipment { get; set; } = new ShipmentModel();

        [RelayCommand]
        private async Task CreateAsync()
        {
            if (IsBusy) return;

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

                // Ya no validamos el rol aquí, porque solo los usuarios regulares pueden llegar a esta pantalla

                string code;
                int attempts = 0;
                do
                {
                    code = TrackingGenerator.NewCode();
                    attempts++;
                    if (attempts > 12) throw new Exception("No se pudo generar un código único.");
                }
                while ((await _shipments.GetByCodeAsync(code)) != null);

                var shipment = new ShipmentModel
                {
                    Code = code,
                    Sender = sender,
                    Worker = null,
                    ReceiverName = ReceiverName?.Trim() ?? "",
                    Status = 1,
                    CreatedDate = DateTime.UtcNow,
                    Description = Description?.Trim() ?? "",
                    Incident = null,
                    Origin = Origin?.Trim() ?? "",
                    Destination = Destination?.Trim() ?? ""
                };

                await _shipments.CreateAsync(shipment);

                try
                {
                    var notif = new NotificationModel
                    {
                        Type = NotificationType.ShipmentCreated,
                        Title = "Paquete creado",
                        Message = $"{shipment.Code} para {shipment.ReceiverName}",
                        Timestamp = DateTime.UtcNow,
                        IsRead = false,
                        ShipmentCode = shipment.Code,
                        DeepLink = $"/{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(shipment.Code)}"
                    };
                    await _notifications.CreateAsync(notif);
                }
                catch (Exception exNotif)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShipmentFormVM] Notification create failed (current user): {exNotif}");
                }

                try
                {
                    await CreateAdminNotificationsAsync(shipment);
                }
                catch (Exception exAdmins)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShipmentFormVM] Admin notifications failed: {exAdmins}");
                }

                Clean();

                await Shell.Current.DisplayAlert("Éxito",
                    $"Envío creado exitosamente.\nCódigo de seguimiento: {shipment.Code}", "OK");

                await Shell.Current.GoToAsync($"/{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(shipment.Code)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo crear el envío: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"[ShipmentForm] Error: {ex}");
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

        private async Task CreateAdminNotificationsAsync(ShipmentModel shipment)
        {
            var allUsers = await _users.GetAllAsync();
            var admins = allUsers.Values.Where(u => u.Role == 1).ToList();
            if (admins.Count == 0) return;

            if (_notifications is INotificationRepositoryMulti multi)
            {
                foreach (var admin in admins)
                {
                    var n = new NotificationModel
                    {
                        Type = NotificationType.ShipmentCreated,
                        Title = "Nuevo envío creado",
                        Message = $"Nuevo envío {shipment.Code} de {shipment.Sender?.Name ?? "Usuario"} desde {shipment.Origin} hacia {shipment.Destination}",
                        Timestamp = DateTime.UtcNow,
                        IsRead = false,
                        ShipmentCode = shipment.Code,
                        DeepLink = $"/{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(shipment.Code)}"
                    };
                    await multi.CreateForUserAsync(admin.Id, n);
                }
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            Origin = "";
            Destination = "";
            ReceiverName = "";
            Description = "";
            await Shell.Current.GoToAsync("..");
        }
    }

    public interface INotificationRepositoryMulti : INotificationRepository
    {
        Task CreateForUserAsync(string userId, NotificationModel n);
    }
}