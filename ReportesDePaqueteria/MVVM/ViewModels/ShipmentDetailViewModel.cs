using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.PlatformConfiguration;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentDetailViewModel : ObservableObject
    {
        private readonly IShipmentRepository _repo;
        private readonly IUserRepository _users;

        [ObservableProperty] private string? code;
        [ObservableProperty] private ShipmentModel? shipment;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool canMarkDelivered; // Para controlar la visibilidad del botón
        [ObservableProperty] private bool canAccessAdminOptions; // Para controlar opciones administrativas

        public ObservableCollection<EventoItem> Eventos { get; } = new();

        public string EstadoText => Shipment?.Status switch
        {
            3 => "Entregado",
            2 => "En tránsito",
            1 => "Enviado",
            _ => "Borrador"
        };

        public DateTime? FechaEnvio => Shipment?.CreatedDate;
        public DateTime? Eta => (Shipment?.CreatedDate)?.AddDays(2);

        public string ServicioText => "Estándar";

        public string? Remitente => Shipment?.Sender?.Name ?? Shipment?.Sender?.Email;
        public string? RemitenteContacto => Shipment?.Sender?.Email;

        public string? Destinatario => Shipment?.ReceiverName;
        public string? DestinatarioContacto => null;

        public string? Origen => Shipment?.Origin;
        public string? Destino => Shipment?.Destination;

        public class BoolFromStringConverter : IValueConverter
        {
            public static BoolFromStringConverter Instance { get; } = new();
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                => value is string s && !string.IsNullOrWhiteSpace(s);
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
        }

        public ShipmentDetailViewModel(IShipmentRepository repo, IUserRepository users)
        {
            _repo = repo;
            _users = users;
        }

        partial void OnCodeChanged(string? value)
        {
            _ = LoadAsync();
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(Code)) return;
            IsBusy = true;
            try
            {
                Shipment = await _repo.GetByCodeAsync(Code!);

                Eventos.Clear();

                if (Shipment is not null)
                {
                    Eventos.Add(new EventoItem("Creado", Shipment.CreatedDate, "Envío registrado"));
                    if (Shipment.Status >= 2) Eventos.Add(new EventoItem("En tránsito", Shipment.CreatedDate.AddHours(12), "Centro de distribución"));
                    if (Shipment.Status == 3) Eventos.Add(new EventoItem("Entregado", Shipment.CreatedDate.AddDays(1), "Recepción exitosa"));

                    // Verificar permisos del usuario actual
                    await UpdateUserPermissionsAsync();
                }

                OnPropertyChanged(nameof(EstadoText));
                OnPropertyChanged(nameof(FechaEnvio));
                OnPropertyChanged(nameof(Eta));
                OnPropertyChanged(nameof(Remitente));
                OnPropertyChanged(nameof(RemitenteContacto));
                OnPropertyChanged(nameof(Destinatario));
                OnPropertyChanged(nameof(DestinatarioContacto));
                OnPropertyChanged(nameof(Origen));
                OnPropertyChanged(nameof(Destino));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateUserPermissionsAsync()
        {
            try
            {
                var currentUserId = await SecureStorage.GetAsync("user_id");
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    CanMarkDelivered = false;
                    CanAccessAdminOptions = false;
                    return;
                }

                var currentUser = await _users.GetByIdAsync(currentUserId);
                if (currentUser == null)
                {
                    CanMarkDelivered = false;
                    CanAccessAdminOptions = false;
                    return;
                }

                //Permisos basados en rol, admin y trabajador marcan como entregado

                bool canMark = false;
                bool canAdmin = false;

                if (currentUser.Role == 1) // Administrador
                {
                    canMark = true;
                    canAdmin = true;
                }
                else if (currentUser.Role == 2) // Trabajador
                {
                    // Solo puede marcar si el envío está asignado a él
                    canMark = Shipment?.Worker?.Id == currentUserId;
                    canAdmin = false;
                }

                // Validacion adicional: solo si el estado no es "Entregado"
                CanMarkDelivered = canMark && Shipment?.Status != 3;
                CanAccessAdminOptions = canAdmin;

                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Usuario {currentUser.Name} (rol {currentUser.Role}): CanMark={CanMarkDelivered}, CanAdmin={CanAccessAdminOptions}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Error checking permissions: {ex.Message}");
                CanMarkDelivered = false;
                CanAccessAdminOptions = false;
            }
        }

        [RelayCommand]
        private async Task MarkDeliveredAsync()
        {
            if (Shipment is null || !CanMarkDelivered) return;

            try
            {

                var confirm = await Shell.Current.DisplayAlert(
                    "Confirmar entrega",
                    $"¿Marcar el envío {Shipment.Code} como entregado?",
                    "Sí, entregar", "Cancelar");

                if (!confirm) return;

                // Actualizar estado
                Shipment.Status = 3; // Entregado
                await _repo.UpdateAsync(Shipment);


                await CreateDeliveryNotificationAsync();

                await LoadAsync();

                await Shell.Current.DisplayAlert("Éxito", "Envío marcado como entregado correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo marcar como entregado: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Error marking as delivered: {ex.Message}");
            }
        }

        private async Task CreateDeliveryNotificationAsync()
        {
            try
            {
                if (Shipment?.Sender == null) return;

                var notification = new NotificationModel
                {
                    Id = DateTime.UtcNow.Ticks.GetHashCode(),
                    Title = "Envío entregado",
                    Message = $"Tu envío {Shipment.Code} ha sido entregado exitosamente a {Shipment.ReceiverName}",
                    IsRead = false,
                    Timestamp = DateTime.UtcNow,
                    ShipmentCode = Shipment.Code,
                };

                var notificationRepo = new NotificationRepository();
                await notificationRepo.CreateAsync(notification);

                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Notificación de entrega creada para {Shipment.Sender.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Error creating delivery notification: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ReportIncidentAsync()
        {
            if (Shipment is null || string.IsNullOrWhiteSpace(Shipment.Code)) return;
            await Shell.Current.GoToAsync($"{nameof(IncidentFormPage)}?shipmentCode={Uri.EscapeDataString(Shipment.Code)}");
        }

        [RelayCommand]
        private Task ShowLabelAsync()
        {
            if (Shipment is null) return Task.CompletedTask;
            return Shell.Current.DisplayAlert("Etiqueta", $"Tracking: {Shipment.Code}", "OK");
        }

        [RelayCommand]
        private Task ShareAsync()
        {
            if (Shipment is null) return Task.CompletedTask;
            return Shell.Current.DisplayAlert("Compartir", $"Se compartiría el tracking {Shipment.Code}", "OK");
        }

        [RelayCommand]
        private async Task MoreOptionsAsync()
        {
            if (Shipment is null) return;

            // Solo mostrar opciones si el usuario tiene permisos administrativos
            if (!CanAccessAdminOptions)
            {
                await Shell.Current.DisplayAlert("Sin opciones", "No tienes opciones disponibles para este envío.", "OK");
                return;
            }

            var options = new List<string> { "Asignar trabajador", "Imprimir", "Borrar" };
            var selectedOption = await Shell.Current.DisplayActionSheet("Opciones", "Cancelar", null, options.ToArray());

            if (selectedOption == "Cancelar" || string.IsNullOrWhiteSpace(selectedOption))
                return;

            switch (selectedOption)
            {
                case "Asignar trabajador":
                    await AssignWorkerAsync();
                    break;
                case "Imprimir":
                    await Shell.Current.DisplayAlert("Imprimir", "Funcionalidad de impresión pendiente.", "OK");
                    break;
                case "Borrar":
                    await DeleteShipmentAsync();
                    break;
            }
        }

        [RelayCommand]
        private async Task AssignWorkerAsync()
        {
            if (Shipment == null || !CanAccessAdminOptions) return;

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
                        Shipment.Worker = workers[workerIndex];
                        Shipment.Status = 2; // Cambiar a "En tránsito"

                        await _repo.UpdateAsync(Shipment);

                        // Crear notificación para el trabajador
                        await CreateWorkerNotificationAsync();

                        await Shell.Current.DisplayAlert("Éxito",
                            $"Trabajador {workers[workerIndex].Name} asignado al envío {Shipment.Code}", "OK");

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

        private async Task CreateWorkerNotificationAsync()
        {
            try
            {
                if (Shipment?.Worker == null) return;

                var notification = new NotificationModel
                {
                    Id = DateTime.UtcNow.Ticks.GetHashCode(),
                    Title = "Nuevo envío asignado",
                    Message = $"Se te ha asignado el envío {Shipment.Code} de {Shipment.Origin} a {Shipment.Destination}",
                    IsRead = false,
                    Timestamp = DateTime.UtcNow,
                    ShipmentCode = Shipment.Code,
                };

                var notificationRepo = new NotificationRepository();
                await notificationRepo.CreateAsync(notification);

                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Notificación creada para trabajador {Shipment.Worker.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipmentDetail] Error al crear notificación: {ex.Message}");
            }
        }

        private async Task DeleteShipmentAsync()
        {
            if (Shipment == null || !CanAccessAdminOptions) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Confirmar eliminación",
                $"¿Eliminar el envío {Shipment.Code}? Esta acción no se puede deshacer.",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            try
            {
                await _repo.DeleteAsync(Shipment.Code);
                await Shell.Current.DisplayAlert("Eliminado", "Envío eliminado correctamente.", "OK");
                await BackAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar el envío: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
            if (nav is null) return;

            if (nav.ModalStack.Count > 0) { await nav.PopModalAsync(); return; }
            if (nav.NavigationStack.Count > 1) { await nav.PopAsync(); return; }

            await nav.PopToRootAsync();
        }
    }

    public record EventoItem(string Titulo, DateTime? Fecha, string? Detalle);
}