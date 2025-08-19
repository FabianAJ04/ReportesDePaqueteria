using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ShipmentListPage : ContentPage
    {
        public ObservableCollection<ShipmentItem> Envios { get; set; }
        public ObservableCollection<string> Estados { get; } =
            new ObservableCollection<string> { "Todos", "Creado", "En tránsito", "Entregado", "Incidencia" };

        public string Search { get; set; }
        public string EstadoSel { get; set; } = "Todos";
        public DateTime FechaDesde { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime FechaHasta { get; set; } = DateTime.Today;

        private ShipmentItem[] _demoBase;

        public ShipmentListPage()
        {
            InitializeComponent();

            _demoBase = new[]
            {
                new ShipmentItem { Id=1, Tracking="PKG-000123", Estado="En tránsito", Origen="Almacén Central", Destino="Juan Pérez, CR", FechaEnvio=DateTime.Today.AddDays(-2) },
                new ShipmentItem { Id=2, Tracking="PKG-000124", Estado="Entregado", Origen="Alajuela", Destino="María Rojas, CR", FechaEnvio=DateTime.Today.AddDays(-5) },
                new ShipmentItem { Id=3, Tracking="PKG-000125", Estado="Incidencia", Origen="Cartago", Destino="Carlos Méndez, CR", FechaEnvio=DateTime.Today.AddDays(-1) },
                new ShipmentItem { Id=4, Tracking="PKG-000126", Estado="Creado", Origen="San José", Destino="Ana López, CR", FechaEnvio=DateTime.Today },
            };

            Envios = new ObservableCollection<ShipmentItem>(_demoBase);
            BindingContext = this;
        }

        private string RutaResumen(ShipmentItem s) => $"{s.Origen} ? {s.Destino}";

        private void RefreshList(ShipmentItem[] baseData)
        {
            var q = baseData.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
                q = q.Where(s => (s.Tracking?.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0)
                              || (s.Destino?.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0));

            if (EstadoSel != "Todos")
                q = q.Where(s => s.Estado == EstadoSel);

            q = q.Where(s => s.FechaEnvio.Date >= FechaDesde.Date && s.FechaEnvio.Date <= FechaHasta.Date);

            var result = q.OrderByDescending(s => s.FechaEnvio)
                          .Select(s => { s.ResumenRuta = RutaResumen(s); return s; })
                          .ToArray();

            Envios.Clear();
            foreach (var it in result) Envios.Add(it);
        }

        private void OnAplicarFiltros(object sender, EventArgs e) => RefreshList(_demoBase);

        private void OnLimpiarFiltros(object sender, EventArgs e)
        {
            Search = string.Empty;
            EstadoSel = "Todos";
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            RefreshList(_demoBase);
        }

        private async void OnNuevoClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ShipmentFormPage));

        private async void OnVerClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ShipmentItem item && item != null)
            {
                var route = $"{nameof(ShipmentDetailPage)}?tracking={Uri.EscapeDataString(item.Tracking)}";
                await Shell.Current.GoToAsync(route);
            }
        }


        private async void OnEtiquetaClicked(object sender, EventArgs e)
            => await DisplayAlert("Etiqueta", "Generación de etiqueta (UI demo).", "OK");
    }

    public class ShipmentItem
    {
        public int Id { get; set; }
        public string Tracking { get; set; }
        public string Estado { get; set; }  
        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime FechaEnvio { get; set; }

        public string ResumenRuta { get; set; }
    }
}
