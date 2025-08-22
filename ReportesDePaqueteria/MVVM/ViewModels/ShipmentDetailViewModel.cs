using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentDetailViewModel : ObservableObject
    {
        private readonly IShipmentRepository _repo;

        [ObservableProperty] private string? code;
        [ObservableProperty] private ShipmentModel? shipment;
        [ObservableProperty] private bool isBusy;

        // Timeline/Eventos para la CollectionView
        public ObservableCollection<EventoItem> Eventos { get; } = new();

        // ---- Proyecciones para el XAML ----
        public string EstadoText => Shipment?.Status switch
        {
            3 => "Entregado",
            2 => "En tránsito",
            1 => "Enviado",
            _ => "Borrador"
        };

        public DateTime? FechaEnvio => Shipment?.CreatedDate;
        public DateTime? Eta => (Shipment?.CreatedDate)?.AddDays(2); // demo: ETA = +2 días

        public string ServicioText => "Estándar"; // TODO: mapear según tu modelo real

        public string? Remitente => Shipment?.Sender?.Name ?? Shipment?.Sender?.Email;
        public string? RemitenteContacto => Shipment?.Sender?.Email;

        public string? Destinatario => Shipment?.ReceiverName;
        public string? DestinatarioContacto => null; 

        public string? Origen => null;  
        public string? Destino => null;  

        public class BoolFromStringConverter : IValueConverter
        {
            public static BoolFromStringConverter Instance { get; } = new();
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                => value is string s && !string.IsNullOrWhiteSpace(s);
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => "";
        }

        public ShipmentDetailViewModel(IShipmentRepository repo)
        {
            _repo = repo;
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

        [RelayCommand]
        private async Task MarkDeliveredAsync()
        {
            if (Shipment is null) return;

            Shipment.Status = 3;
            await _repo.UpdateAsync(Shipment);

            await LoadAsync();
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
        private Task MoreOptionsAsync()
        {
            return Shell.Current.DisplayActionSheet("Opciones", "Cancelar", null, "Asignar trabajador", "Imprimir", "Borrar");
        }

        [RelayCommand]
        private async Task AssignWorkerAsync(UserModel? worker)
        {
            if (Shipment == null || worker == null) return;
            Shipment.Worker = worker;
            await _repo.UpdateAsync(Shipment);
            await LoadAsync();
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
