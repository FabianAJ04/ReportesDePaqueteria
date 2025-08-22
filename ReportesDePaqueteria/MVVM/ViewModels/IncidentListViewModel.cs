using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;                        
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class IncidentListViewModel : ObservableObject
    {
        private readonly IIncidentRepository _incidents;

        // Estado / refresco
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;

        // Búsqueda y filtros 
        [ObservableProperty] private string? searchText;

        public ObservableCollection<string> Categorias { get; } =
            new(new[] { "Todas", "Paquete", "Entrega", "Pago", "Otro" });

        public ObservableCollection<string> Estados { get; } =
            new(new[] { "Todos", "Abierto", "En progreso", "Resuelto", "Cerrado" });  // Los usuarios no deberian de asignar Estado a los incidentes

        public ObservableCollection<string> Prioridades { get; } =                  // Los usuarios no deberian de asignar Prioridad a los incidentes
            new(new[] { "Todas", "Baja", "Media", "Alta", "Crítica" });

        public ObservableCollection<string> Impactos { get; } =
            new(new[] { "Todos" });

        [ObservableProperty] private string categoriaSel = "Todas";
        [ObservableProperty] private string estadoSel = "Todos";
        [ObservableProperty] private string prioridadSel = "Todas";
        [ObservableProperty] private string impactoSel = "Todos";

        [ObservableProperty] private DateTime fechaDesde = DateTime.Today.AddDays(-30);
        [ObservableProperty] private DateTime fechaHasta = DateTime.Today;

        // Lista que enlaza la UI (IncidentModel directamente)
        public ObservableCollection<IncidentModel> Incidentes { get; } = new();

        // Respaldo completo para filtrar
        private readonly List<IncidentModel> _allModels = new();

        public IncidentListViewModel(IIncidentRepository incidents)
        {
            _incidents = incidents ?? throw new ArgumentNullException(nameof(incidents));

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(SearchText)
                    or nameof(CategoriaSel)
                    or nameof(EstadoSel)
                    or nameof(PrioridadSel)
                    or nameof(ImpactoSel)
                    or nameof(FechaDesde)
                    or nameof(FechaHasta))
                {
                    ApplyFilter();
                }
            };
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
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                _allModels.Clear();
                Incidentes.Clear();

                var dict = await _incidents.GetAllAsync();
                foreach (var m in dict.Values.OrderByDescending(i => i.DateTime))
                {
                    m.Title ??= string.Empty;
                    m.Description ??= string.Empty;
                    m.ShipmentCode ??= string.Empty;
                    _allModels.Add(m);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false; 
            }
        }

        [RelayCommand]
        private void ApplyFilters() => ApplyFilter();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            CategoriaSel = "Todas";
            EstadoSel = "Todos";
            PrioridadSel = "Todas";
            ImpactoSel = "Todos";
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            ApplyFilter();
        }

        [RelayCommand]
        private async Task OpenAsync(IncidentModel? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync($"{nameof(IncidentDetailPage)}?id={item.Id}");
        }

        private void ApplyFilter()
        {
            Incidentes.Clear();

            var start = FechaDesde.Date;
            var end = FechaHasta.Date.AddDays(1).AddTicks(-1);
            var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();

            int? cat = MapCategoryNameToInt(CategoriaSel);
            int? sts = MapStatusNameToInt(EstadoSel);
            int? pri = MapPriorityNameToInt(PrioridadSel);

            IEnumerable<IncidentModel> query = _allModels;

            // Fecha
            query = query.Where(i => i.DateTime >= start && i.DateTime <= end);

            // Categoría
            if (cat.HasValue)
                query = query.Where(i => i.Category == cat.Value);

            // Estado
            if (sts.HasValue)
                query = query.Where(i => i.Status == sts.Value);

            // Prioridad
            if (pri.HasValue)
                query = query.Where(i => i.Priority == pri.Value);

            // Búsqueda en Title/Description/Assignee
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(i =>
                    (i.Title ?? "").ToLowerInvariant().Contains(q) ||
                    (i.Description ?? "").ToLowerInvariant().Contains(q) ||
                    ((i.Assignee?.Name ?? i.Assignee?.Email ?? i.AssigneeId ?? "")
                        .ToLowerInvariant()
                        .Contains(q)));
            }

            foreach (var m in query)
                Incidentes.Add(m);
        }

        private static int? MapCategoryNameToInt(string name) => name switch
        {
            "Paquete" => 1,
            "Entrega" => 2,
            "Pago" => 3,
            "Otro" => 4,
            _ => null  // "Todas"
        };

        private static int? MapStatusNameToInt(string name) => name switch
        {
            "Abierto" => 1,
            "En progreso" => 2,
            "Resuelto" => 3,
            "Cerrado" => 4,
            _ => null // "Todos"
        };

        private static int? MapPriorityNameToInt(string name) => name switch
        {
            "Baja" => 1,
            "Media" => 2,
            "Alta" => 3,
            "Crítica" => 4,
            _ => null // "Todas"
        };
    }
}
