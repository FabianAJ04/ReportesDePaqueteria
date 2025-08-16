using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class NotificationsPage : ContentPage
    {
        public ObservableCollection<NotificacionItem> Notificaciones { get; set; }
        public string Search { get; set; }

        public ObservableCollection<string> Estados { get; } =
            new ObservableCollection<string> { "Todas", "No leídas" };
        public ObservableCollection<string> Tipos { get; } =
            new ObservableCollection<string> { "Todos", "Incidente", "Asignación", "Sistema" };
        public string EstadoSel { get; set; } = "Todas";
        public string TipoSel { get; set; } = "Todos";

        public NotificationsPage()
        {
            InitializeComponent();

            Notificaciones = new ObservableCollection<NotificacionItem>
            {
                new NotificacionItem
                {
                    Id = 1,
                    Titulo = "Incidente asignado",
                    Mensaje = "Se te asignó el incidente #102 Fuga de agua en piso 3.",
                    Tipo = "Asignación",
                    Fecha = DateTime.Now.AddMinutes(-15),
                    Leida = false,
                    Icono = "assign.png" 
                },
                new NotificacionItem
                {
                    Id = 2,
                    Titulo = "Estado actualizado",
                    Mensaje = "Incidente #95 cambió a 'En proceso'.",
                    Tipo = "Incidente",
                    Fecha = DateTime.Now.AddHours(-3),
                    Leida = false,
                    Icono = "incident.png"
                },
                new NotificacionItem
                {
                    Id = 3,
                    Titulo = "Mantenimiento programado",
                    Mensaje = "El sistema estará en mantenimiento esta noche a las 23:00.",
                    Tipo = "Sistema",
                    Fecha = DateTime.Now.AddDays(-1),
                    Leida = true,
                    Icono = "system.png"
                }
            };

            BindingContext = this;
        }

        private void OnMarkAllReadClicked(object sender, EventArgs e)
        {
            foreach (var n in Notificaciones) n.Leida = true;
            var items = Notificaciones.ToList();
            Notificaciones.Clear();
            foreach (var it in items) Notificaciones.Add(it);
        }

        private void OnClearAllClicked(object sender, EventArgs e)
        {
            Notificaciones.Clear();
        }

        private void OnApplyFiltersClicked(object sender, EventArgs e)
        {
           
            var baseData = new[]
            {
                new NotificacionItem { Id=1, Titulo="Incidente asignado", Mensaje="Se te asignó el incidente #102 Fuga de agua en piso 3.", Tipo="Asignación", Fecha=DateTime.Now.AddMinutes(-15), Leida=false, Icono="assign.png" },
                new NotificacionItem { Id=2, Titulo="Estado actualizado", Mensaje="Incidente #95 cambió a 'En proceso'.", Tipo="Incidente", Fecha=DateTime.Now.AddHours(-3), Leida=false, Icono="incident.png" },
                new NotificacionItem { Id=3, Titulo="Mantenimiento programado", Mensaje="El sistema estará en mantenimiento esta noche a las 23:00.", Tipo="Sistema", Fecha=DateTime.Now.AddDays(-1), Leida=true, Icono="system.png" }
            }.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
                baseData = baseData.Where(n => (n.Titulo?.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0)
                                            || (n.Mensaje?.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0));

            if (EstadoSel == "No leídas")
                baseData = baseData.Where(n => !n.Leida);

            if (TipoSel != "Todos")
                baseData = baseData.Where(n => n.Tipo == TipoSel);

            Notificaciones.Clear();
            foreach (var n in baseData.OrderByDescending(n => n.Fecha))
                Notificaciones.Add(n);
        }

        private void OnMarkReadSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is NotificacionItem n)
            {
                n.Leida = true;
                var items = Notificaciones.ToList();
                Notificaciones.Clear();
                foreach (var it in items) Notificaciones.Add(it);
            }
        }

        private void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is NotificacionItem n)
                Notificaciones.Remove(n);
        }
    }

    public class NotificacionItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string Tipo { get; set; } 
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
        public string Icono { get; set; }
    }
}
