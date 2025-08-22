using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;
using System.Collections.ObjectModel;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    [QueryProperty(nameof(ShipmentCode), "shipmentCode")]
    public partial class IncidentFormViewModel : ObservableObject
    {
        private readonly IIncidentRepository _incidents;
        private readonly IShipmentRepository _shipments;
        private readonly IUserRepository _users;

        public IncidentFormViewModel(IIncidentRepository incidents,
                                     IShipmentRepository shipments,
                                     IUserRepository users)
        {
            _incidents = incidents;
            _shipments = shipments;
            _users = users;

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
                // 1) Cargar envío
                if (!string.IsNullOrWhiteSpace(Incident.ShipmentCode))
                    Shipment = await _shipments.GetByCodeAsync(Incident.ShipmentCode);

                // 2) user_id -> CreatedById
                var uid = await SecureStorage.GetAsync("user_id");
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    if (Incident.CreatedById != uid)
                    {
                        Incident.CreatedById = uid;
                        OnPropertyChanged(nameof(Incident));
                    }
                }

                // 3) sugerir título si falta
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
                Incident.DateTime = DateTime.UtcNow;
                await _incidents.CreateAsync(Incident);
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
