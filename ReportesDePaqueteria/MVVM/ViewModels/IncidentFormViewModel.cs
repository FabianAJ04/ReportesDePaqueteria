using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    [QueryProperty(nameof(ShipmentCode), "shipmentCode")]
    public partial class IncidentFormViewModel : ObservableObject
    {
        private readonly IIncidentRepository _incidents;
        private readonly IShipmentRepository _shipments;
        private readonly IUserRepository _users;

        private readonly INotificationRepository _notifications;

        public IncidentFormViewModel(IIncidentRepository incidents,
                                     IShipmentRepository shipments,
                                     IUserRepository users,
                                     INotificationRepository notifications) 
        {
            _incidents = incidents;
            _shipments = shipments;
            _users = users;
            _notifications = notifications; 

            Incident = new IncidentModel();

            StatusOptions = new(new[] { "Abierto", "En progreso", "Resuelto", "Cerrado" });
            PriorityOptions = new(new[] { "Baja", "Media", "Alta", "Crítica" });
            CategoryOptions = new(new[] { "Problema del paquete", "Problema de entrega", "Problema de pago", "Otro" });
        }

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private IncidentModel incident;

        [ObservableProperty] private string? shipmentCode;
        partial void OnShipmentCodeChanged(string? value)
        {
            Incident.ShipmentCode = value ?? "";
            _ = PrefillAsync();
        }

        [ObservableProperty] private ShipmentModel? shipment;

        public ObservableCollection<string> StatusOptions { get; }
        public ObservableCollection<string> PriorityOptions { get; }
        public ObservableCollection<string> CategoryOptions { get; }

        public async Task PrefillAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(Incident.ShipmentCode))
                    Shipment = await _shipments.GetByCodeAsync(Incident.ShipmentCode);

                var uid = await SecureStorage.GetAsync("user_id");
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    Incident.CreatedById = uid;
                    OnPropertyChanged(nameof(Incident));
                }

                if (string.IsNullOrWhiteSpace(Incident.Title))
                    Incident.Title = string.IsNullOrWhiteSpace(Incident.ShipmentCode)
                        ? "Incidente"
                        : $"Incidente del envío {Incident.ShipmentCode}";

                OnPropertyChanged(nameof(Incident));
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Incident.Title))
            {
                await Shell.Current.DisplayAlert("Validación", "El título es requerido.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                Incident.Id = 0;

                Incident.DateTime = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(Incident.CreatedById))
                {
                    var uid = await SecureStorage.GetAsync("user_id");
                    if (!string.IsNullOrWhiteSpace(uid))
                        Incident.CreatedById = uid;
                }

                // 1) Crear incidente
                await _incidents.CreateAsync(Incident);

                // 2) Crear notificación 
                try
                {
                    var notif = new NotificationModel
                    {
                        Type = NotificationType.IncidentCreated,
                        Title = string.IsNullOrWhiteSpace(Incident.Title) ? "Nuevo incidente" : Incident.Title,
                        Message = string.IsNullOrWhiteSpace(Incident.ShipmentCode)
                                    ? $"Se creó el incidente #{Incident.Id}."
                                    : $"Se creó el incidente #{Incident.Id} del envío {Incident.ShipmentCode}.",
                        Timestamp = DateTime.UtcNow,
                        IsRead = false,
                        IncidentId = Incident.Id,
                        ShipmentCode = Incident.ShipmentCode,
                        DeepLink = $"/IncidentDetailPage?id={Incident.Id}"
                    };

                    await _notifications.CreateAsync(notif);
                }
                catch (Exception exNotif)
                {
                    System.Diagnostics.Debug.WriteLine($"[IncidentFormVM] Notification create failed: {exNotif}");
                }

                await Shell.Current.DisplayAlert("Éxito", "Incidente creado.", "OK");
                await BackAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo crear el incidente: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
            if (nav is null) return;
            if (nav.ModalStack.Count > 0) { await nav.PopModalAsync(); return; }
            if (nav.NavigationStack.Count > 1) { await nav.PopAsync(); return; }
            await Shell.Current.GoToAsync("..");
        }
    }
}
