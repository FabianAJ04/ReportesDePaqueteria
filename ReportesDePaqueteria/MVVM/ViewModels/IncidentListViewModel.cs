using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Firebase.Database.Streaming;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using ReportesDePaqueteria.MVVM.Messaging;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class IncidentListViewModel :
        ObservableRecipient,
        IRecipient<IncidentSavedMessage>,
        IRecipient<IncidentDeletedMessage>
    {
        private readonly IIncidentRepository _incidents;
        private readonly IUserRepository _users;
        private IDisposable? _liveSub;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string? searchText;
        [ObservableProperty] private bool canViewDetails = true; // Para controlar visibilidad del botón "Ver"

        public ObservableCollection<string> Categorias { get; } =
            new(new[] { "Todas", "Paquete", "Entrega", "Pago", "Otro" });

        public ObservableCollection<string> Estados { get; } =
            new(new[] { "Todos", "Abierto", "En progreso", "Resuelto", "Cerrado" });

        public ObservableCollection<string> Prioridades { get; } =
            new(new[] { "Todas", "Baja", "Media", "Alta", "Crítica" });

        public ObservableCollection<string> Impactos { get; } =
            new(new[] { "Todos" });

        [ObservableProperty] private string categoriaSel = "Todas";
        [ObservableProperty] private string estadoSel = "Todos";
        [ObservableProperty] private string prioridadSel = "Todas";
        [ObservableProperty] private string impactoSel = "Todos";

        [ObservableProperty] private DateTime fechaDesde = DateTime.Today.AddDays(-30);
        [ObservableProperty] private DateTime fechaHasta = DateTime.Today;

        public ObservableCollection<IncidentModel> Incidentes { get; } = new();
        private readonly List<IncidentModel> _all = new();

        // Variables para el filtrado por usuario
        private string? _currentUserId;
        private int _currentUserRole = 3; // Por defecto usuario normal

        public IncidentListViewModel(IIncidentRepository incidents, IUserRepository users)
        {
            _incidents = incidents ?? throw new ArgumentNullException(nameof(incidents));
            _users = users ?? throw new ArgumentNullException(nameof(users));
            IsActive = true;

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SearchText) ||
                    e.PropertyName == nameof(EstadoSel) ||
                    e.PropertyName == nameof(PrioridadSel) ||
                    e.PropertyName == nameof(CategoriaSel))
             {
                    ApplyFilter();
                }
            };
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            _liveSub ??= _incidents.ObserveAll().Subscribe(OnFirebaseEvent, OnFirebaseError);
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            _liveSub?.Dispose();
            _liveSub = null;
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
                // NUEVO: Obtener información del usuario actual
                await LoadCurrentUserInfoAsync();

                _all.Clear();
                Incidentes.Clear();

                var dict = await _incidents.GetAllAsync();

                // CAMBIO PRINCIPAL: Filtrar incidentes según el rol del usuario
                var filteredIncidents = dict.Values.AsEnumerable();

                if (_currentUserRole == 3) // Si es usuario normal (Role = 3)
                {
                    // Solo mostrar sus propios incidentes
                    filteredIncidents = filteredIncidents.Where(i =>
                        !string.IsNullOrEmpty(i.CreatedById) &&
                        i.CreatedById == _currentUserId);
                }
                // Para Admin (1) y Trabajador (2), mostrar todos los incidentes (sin filtro)

                foreach (var m in filteredIncidents.OrderByDescending(i => i.DateTime))
                {
                    m.Title ??= string.Empty;
                    m.Description ??= string.Empty;
                    m.ShipmentCode ??= string.Empty;
                    _all.Add(m);
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

        // NUEVO MÉTODO: Cargar información del usuario actual
        private async Task LoadCurrentUserInfoAsync()
        {
            try
            {
                _currentUserId = await SecureStorage.GetAsync("user_id");

                if (!string.IsNullOrWhiteSpace(_currentUserId))
                {
                    var currentUser = await _users.GetByIdAsync(_currentUserId);
                    _currentUserRole = currentUser?.Role ?? 3; // Por defecto usuario normal

                    // Actualizar la propiedad para controlar visibilidad del botón "Ver"
                    // Solo Admin (1) y Trabajador (2) pueden ver detalles
                    CanViewDetails = _currentUserRole == 1 || _currentUserRole == 2;
                }
                else
                {
                    _currentUserRole = 3; // Por defecto usuario normal
                    CanViewDetails = false;
                }

                System.Diagnostics.Debug.WriteLine($"[IncidentListVM] Current user: {_currentUserId}, Role: {_currentUserRole}, CanView: {CanViewDetails}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentListVM] Error loading user info: {ex.Message}");
                _currentUserRole = 3;
                CanViewDetails = false;
            }
        }

        [RelayCommand]
        private async Task OpenAsync(IncidentModel? item)
        {
            if (item is null || item.Id <= 0) return;

            // NUEVA VALIDACIÓN: Solo permitir acceso si el usuario tiene permisos
            if (!CanViewDetails)
            {
                await Shell.Current.DisplayAlert("Acceso denegado",
                    "No tienes permisos para ver los detalles del incidente.", "OK");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(IncidentDetailPage)}?id={item.Id}");
        }

        private void ApplyFilter()
        {
            var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();

            var query = _all.AsEnumerable();

            // Filtro por texto
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(i =>
                    (i.Title ?? "").ToLowerInvariant().Contains(q) ||
                    (i.Description ?? "").ToLowerInvariant().Contains(q) ||
                    ((i.Assignee?.Name ?? i.Assignee?.Email ?? i.AssigneeId ?? "")
                        .ToLowerInvariant()
                        .Contains(q)));
            }

            // Filtro por Estado
            if (EstadoSel != "Todos")
            {
                query = query.Where(i =>
                    (i.Status == 1 && EstadoSel == "Abierto") ||
                    (i.Status == 2 && EstadoSel == "En progreso") ||
                    (i.Status == 3 && EstadoSel == "Resuelto") ||
                    (i.Status == 4 && EstadoSel == "Cerrado"));
            }

            // Filtro por Prioridad
            if (PrioridadSel != "Todas")
            {
                query = query.Where(i =>
                    (i.Priority == 1 && PrioridadSel == "Baja") ||
                    (i.Priority == 2 && PrioridadSel == "Media") ||
                    (i.Priority == 3 && PrioridadSel == "Alta") ||
                    (i.Priority == 4 && PrioridadSel == "Crítica"));
            }

            // Filtro por Categoría
            if (CategoriaSel != "Todas")
            {
                query = query.Where(i =>
                    (i.Category == 1 && CategoriaSel == "Paquete") ||
                    (i.Category == 2 && CategoriaSel == "Entrega") ||
                    (i.Category == 3 && CategoriaSel == "Pago") ||
                    (i.Category == 4 && CategoriaSel == "Otro"));
            }

            // Cargar a la colección observable
            Incidentes.Clear();
            foreach (var m in query)
                Incidentes.Add(m);
        }


        public void Receive(IncidentSavedMessage message)
        {
            var updated = message.Value;
            if (updated is null) return;
            UpsertLocal(updated);
        }

        public void Receive(IncidentDeletedMessage message)
        {
            var id = message.Value;
            RemoveLocal(id);
        }

        private void OnFirebaseEvent(FirebaseEvent<IncidentModel> ev)
        {
            try
            {
                if (ev is null) return;
                var model = ev.Object;
                if (model is null) return;

                if (model.Id <= 0 && int.TryParse(ev.Key, out var parsed))
                    model.Id = parsed;

                switch (ev.EventType)
                {
                    case FirebaseEventType.InsertOrUpdate:
                        UpsertLocal(model);
                        break;

                    case FirebaseEventType.Delete:
                        var delId = model.Id > 0 ? model.Id
                                  : (int.TryParse(ev.Key, out var k) ? k : 0);
                        if (delId > 0) RemoveLocal(delId);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentListVM] live event error: {ex}");
            }
        }

        private void OnFirebaseError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IncidentListVM] live stream error: {ex}");
        }

        private void UpsertLocal(IncidentModel updated)
        {
            // NUEVA LÓGICA: Aplicar filtro de usuario también en las actualizaciones en vivo
            bool shouldInclude = true;

            if (_currentUserRole == 3) // Si es usuario normal
            {
                shouldInclude = !string.IsNullOrEmpty(updated.CreatedById) &&
                               updated.CreatedById == _currentUserId;
            }

            if (!shouldInclude) return; // No incluir este incidente para el usuario actual

            var idx = _all.FindIndex(x => x.Id == updated.Id);
            if (idx >= 0) _all[idx] = updated;
            else _all.Insert(0, updated);

            _all.Sort((a, b) => b.DateTime.CompareTo(a.DateTime));
            MainThread.BeginInvokeOnMainThread(ApplyFilter);
        }

        private void RemoveLocal(int id)
        {
            _all.RemoveAll(x => x.Id == id);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var item = Incidentes.FirstOrDefault(x => x.Id == id);
                if (item != null) Incidentes.Remove(item);
            });
        }
    }
}