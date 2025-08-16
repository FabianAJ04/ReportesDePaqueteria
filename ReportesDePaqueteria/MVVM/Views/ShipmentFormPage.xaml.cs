using Microsoft.Maui.Controls;
using System;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ShipmentFormPage : ContentPage
    {
        public ShipmentFormVM VM { get; set; } = new ShipmentFormVM();

        public ShipmentFormPage()
        {
            InitializeComponent();
            BindingContext = VM;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VM.Origen) || string.IsNullOrWhiteSpace(VM.Destino))
            {
                await DisplayAlert("Campos requeridos", "", "OK");
                return;
            }

            await DisplayAlert("Envío", ".", "OK");
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }

    public class ShipmentFormVM
    {
        public string Tracking { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.Today;
        public string Notas { get; set; }
        public bool Seguro { get; set; }
        public bool ContraEntrega { get; set; }
    }
}
