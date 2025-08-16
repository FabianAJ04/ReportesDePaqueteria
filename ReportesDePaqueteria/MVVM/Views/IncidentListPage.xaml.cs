using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class IncidentListPage : ContentPage
    {
        public ObservableCollection<IncidentListItem> Incidentes { get; set; }

        public IncidentListPage()
        {
            InitializeComponent();

            // Datos de ejemplo
            Incidentes = new ObservableCollection<IncidentListItem>
            {
                new IncidentListItem
                {
                    Id = 101,
                    Titulo = "Fuga de agua en piso 3",
                    Descripcion = "Se detecta fuga en conducto principal. �rea parcialmente inundada.",
                    Fecha = DateTime.Now.AddHours(-5),
                    Categoria = "Infraestructura",
                    Prioridad = "Alta",
                    Estado = "En proceso",
                    Impacto = "Alto",
                    Responsable = "Equipo Mantenimiento",
                    Ubicacion = "Edificio Central - Piso 3",
                    PersonasInvolucradas = "Carlos M�ndez, Ana Rojas"
                },
                new IncidentListItem
                {
                    Id = 102,
                    Titulo = "Falla el�ctrica sector B",
                    Descripcion = "Cortes intermitentes de energ�a.",
                    Fecha = DateTime.Now.AddHours(-10),
                    Categoria = "El�ctrico",
                    Prioridad = "Media",
                    Estado = "Abierto",
                    Impacto = "Medio",
                    Responsable = "Ingenier�a",
                    Ubicacion = "Planta Norte - Sector B",
                    PersonasInvolucradas = "Operador Turno Noche"
                },
                new IncidentListItem
                {
                    Id = 103,
                    Titulo = "Acceso bloqueado a aplicaci�n",
                    Descripcion = "Usuarios reportan error 403 al iniciar sesi�n.",
                    Fecha = DateTime.Now.AddDays(-1),
                    Categoria = "TI",
                    Prioridad = "Baja",
                    Estado = "Resuelto",
                    Impacto = "Bajo",
                    Responsable = "Soporte TI",
                    Ubicacion = "Remoto",
                    PersonasInvolucradas = "Mesa de Ayuda"
                }
            };

            BindingContext = this; 
        }

        private async void OnVerClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is IncidentListItem item)
            {
                await Shell.Current.GoToAsync(nameof(IncidentDetailPage), new Dictionary<string, object>
                {
                    { "item", item }
                });
            }
        }

    }

    public class IncidentListItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public string Categoria { get; set; }
        public string Prioridad { get; set; }
        public string Estado { get; set; }
        public string Impacto { get; set; }
        public string Responsable { get; set; }
        public string Ubicacion { get; set; }
        public string PersonasInvolucradas { get; set; }
    }
}
