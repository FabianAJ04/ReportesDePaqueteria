using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;

namespace ReportesDePaqueteria.MVVM.Views
{
    [QueryProperty(nameof(TrackingParam), "tracking")]
    public partial class ShipmentDetailPage : ContentPage
    {
        public ShipmentDetailVM VM { get; set; } = new ShipmentDetailVM();

        public string TrackingParam
        {
            get => VM?.Tracking;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                LoadDemo(value);          
                BindingContext = VM;      
            }
        }

        public ShipmentDetailPage()
        {
            InitializeComponent();

          
            BindingContext = VM;
        }

        private void LoadDemo(string tracking)
        {
            VM.Tracking = tracking;
            VM.Estado = "En tránsito";          
            VM.Servicio = "Estándar 48h";
            VM.FechaEnvio = DateTime.Today.AddDays(-1);
            VM.EstimadoEntrega = DateTime.Today.AddDays(1);

            VM.Origen = "Almacén Central, San José";
            VM.Destino = "Juan Pérez, Heredia";
            VM.Remitente = "Logística CR S.A.";
            VM.RemitenteContacto = "4000-0000 • soporte@logcr.cr";
            VM.Destinatario = "Juan Pérez";
            VM.DestinatarioContacto = "8888-8888 • juanp@gmail.com";

            VM.Peso = "2.3 kg";
            VM.Dimensiones = "30 x 20 x 15 cm";
            VM.Contenido = "Componentes electrónicos (frágil)";

            VM.Eventos.Clear();
            VM.Eventos.Add(new EventoTracking { Titulo = "Recogido", Detalle = "Paquete retirado en origen", Fecha = DateTime.Now.AddHours(-28) });
            VM.Eventos.Add(new EventoTracking { Titulo = "En tránsito", Detalle = "En ruta hacia centro de distribución", Fecha = DateTime.Now.AddHours(-16) });
            VM.Eventos.Add(new EventoTracking { Titulo = "En centro de distribución", Detalle = "Clasificación en progreso", Fecha = DateTime.Now.AddHours(-8) });
        }

        private async void OnVerEtiquetaClicked(object sender, EventArgs e)
            => await DisplayAlert("Etiqueta", "Mostrar/descargar etiqueta o QR (demo).", "OK");

        private async void OnReportarIncidenciaClicked(object sender, EventArgs e)
            => await DisplayAlert("Incidencia", "Abrir reporte de incidencia para este envío (demo).", "OK");

        private async void OnMarcarEntregadoClicked(object sender, EventArgs e)
        {
            VM.Estado = "Entregado";
            VM.Eventos.Insert(0, new EventoTracking
            {
                Titulo = "Entregado",
                Detalle = "Entregado al destinatario",
                Fecha = DateTime.Now
            });
            await DisplayAlert("Estado", "El envío fue marcado como ENTREGADO.", "OK");
        }

        private async void OnCompartirClicked(object sender, EventArgs e)
            => await DisplayAlert("Compartir", $"Compartir tracking {VM.Tracking} (demo).", "OK");

        private async void OnMasOpcionesClicked(object sender, EventArgs e)
            => await DisplayActionSheet("Opciones", "Cancelar", null, "Reprogramar entrega", "Generar recolección", "Contactar soporte");
    }

    public class ShipmentDetailVM : BindableObject
    {
        private string _estado;

        // Header
        public string Tracking { get; set; }
        public string Estado
        {
            get => _estado;
            set { _estado = value; OnPropertyChanged(); }
        }
        public string Servicio { get; set; }
        public DateTime FechaEnvio { get; set; }
        public DateTime EstimadoEntrega { get; set; }

        // Ruta
        public string Origen { get; set; }
        public string Destino { get; set; }

        // Rem/Dest
        public string Remitente { get; set; }
        public string RemitenteContacto { get; set; }
        public string Destinatario { get; set; }
        public string DestinatarioContacto { get; set; }

        // Paquete
        public string Peso { get; set; }
        public string Dimensiones { get; set; }
        public string Contenido { get; set; }

        // Seguimiento
        public ObservableCollection<EventoTracking> Eventos { get; } = new();
    }

    public class EventoTracking
    {
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public DateTime Fecha { get; set; }
    }
}
