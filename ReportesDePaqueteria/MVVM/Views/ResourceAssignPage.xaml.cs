using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ResourceAssignPage : ContentPage
    {
        public ResourceAssignVM VM { get; } = new ResourceAssignVM();

        public ResourceAssignPage()
        {
            InitializeComponent();
            BindingContext = VM;
        }

        // Tabs
        private void OnTabPersonas(object sender, EventArgs e) { VM.SetTipoActivo(ResourceType.Persona); }
        private void OnTabPaquetes(object sender, EventArgs e) { VM.SetTipoActivo(ResourceType.Paquete); }
        private void OnTabEquipos(object sender, EventArgs e) { VM.SetTipoActivo(ResourceType.Equipo); }

        // Asignar
        private async void OnAsignarClicked(object sender, EventArgs e)
        {
            var sel = VM.Items.Where(i => i.Seleccionado).ToList();
            if (!sel.Any())
            {
                await DisplayAlert("Asignación", "Selecciona al menos un recurso.", "OK");
                return;
            }

            var resumen = string.Join("\n", sel.Select(s => $"• [{s.Tipo}] {s.Nombre} ({s.Detalle})"));
            await DisplayAlert("Asignados", $"Se asignaron:\n{resumen}", "OK");

            // Si vienes desde un incidente, aquí podrías devolver datos por MessagingCenter/WeakReferenceMessenger
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCloseClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }

    public enum ResourceType { Persona, Paquete, Equipo }

    public class ResourceAssignVM : BindableObject
    {
        public ObservableCollection<ResourceItem> Items { get; } = new();
        public ObservableCollection<ResourceItem> ItemsFiltrados { get; } = new();

        private string _search;
        public string Search
        {
            get => _search;
            set { _search = value; OnPropertyChanged(); ApplyFilters(); }
        }

        // Tabs
        public bool PersonasActiva { get; private set; } = true;
        public bool PaquetesActiva { get; private set; }
        public bool EquiposActiva { get; private set; }
        private ResourceType _tipoActivo = ResourceType.Persona;

        public string SeleccionadosTexto => $"{Items.Count(i => i.Seleccionado)} seleccionados";

        public ResourceAssignVM()
        {
            // Datos demo
            Items.Add(new ResourceItem { Id = 1, Tipo = "Persona", Nombre = "Ana Rojas", Detalle = "Técnico de soporte", Estado = "Disponible", Icono = "user.png" });
            Items.Add(new ResourceItem { Id = 2, Tipo = "Persona", Nombre = "Carlos Méndez", Detalle = "Ingeniero de campo", Estado = "Ocupado", Icono = "user.png" });

            Items.Add(new ResourceItem { Id = 10, Tipo = "Paquete", Nombre = "PKG-000124", Detalle = "2.3 kg • Fragil", Estado = "En almacén", Icono = "box.png" });
            Items.Add(new ResourceItem { Id = 11, Tipo = "Paquete", Nombre = "PKG-000125", Detalle = "1.1 kg • Estándar", Estado = "Ruta", Icono = "box.png" });

            Items.Add(new ResourceItem { Id = 20, Tipo = "Equipo", Nombre = "Vehículo CR-984", Detalle = "Hilux 4x4 • Placa 12345", Estado = "Disponible", Icono = "truck.png" });
            Items.Add(new ResourceItem { Id = 21, Tipo = "Equipo", Nombre = "Dron #7", Detalle = "Batería 70%", Estado = "Mantenimiento", Icono = "gear.png" });

            ApplyFilters();
        }

        public void SetTipoActivo(ResourceType t)
        {
            _tipoActivo = t;
            PersonasActiva = (t == ResourceType.Persona);
            PaquetesActiva = (t == ResourceType.Paquete);
            EquiposActiva = (t == ResourceType.Equipo);
            OnPropertyChanged(nameof(PersonasActiva));
            OnPropertyChanged(nameof(PaquetesActiva));
            OnPropertyChanged(nameof(EquiposActiva));
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var tipoStr = _tipoActivo switch
            {
                ResourceType.Persona => "Persona",
                ResourceType.Paquete => "Paquete",
                ResourceType.Equipo => "Equipo",
                _ => "Persona"
            };

            var q = Items.Where(i => i.Tipo == tipoStr);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim();
                q = q.Where(i =>
                    (i.Nombre?.IndexOf(s, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (i.Detalle?.IndexOf(s, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            ItemsFiltrados.Clear();
            foreach (var it in q) ItemsFiltrados.Add(it);

            OnPropertyChanged(nameof(SeleccionadosTexto));
        }
    }

    public class ResourceItem : BindableObject
    {
        public int Id { get; set; }
        public string Tipo { get; set; }      // Persona | Paquete | Equipo
        public string Nombre { get; set; }
        public string Detalle { get; set; }   // Rol, placa, peso, etc.
        public string Estado { get; set; }
        public string Icono { get; set; }

        private bool _seleccionado;
        public bool Seleccionado
        {
            get => _seleccionado;
            set { _seleccionado = value; OnPropertyChanged(); }
        }
    }

    /// <summary>
    /// Convierte bool -> Style (activo/inactivo) usando ConverterParameter "ActiveKey|DefaultKey"
    /// </summary>
    public sealed class BoolToStyleConverter : IValueConverter
    {
        public static BoolToStyleConverter Instance { get; } = new BoolToStyleConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var keys = (parameter as string)?.Split('|');
            if (keys == null || keys.Length != 2) return null;

            var activeKey = keys[0];
            var defaultKey = keys[1];
            var isActive = value is bool b && b;

            var app = Application.Current;
            if (app?.Resources == null) return null;

            return isActive ? app.Resources[activeKey] as Style
                            : app.Resources[defaultKey] as Style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}
