using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Messaging;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    [QueryProperty(nameof(Id), "id")]
    public partial class IncidentDetailViewModel : ObservableObject
    {
        private readonly IIncidentRepository _incidents;
        private readonly IUserRepository _users;

        public IncidentDetailViewModel(IIncidentRepository incidents, IUserRepository users)
        {
            _incidents = incidents;
            _users = users;

            StatusOptions.Add(new Option(1, "Abierto"));
            StatusOptions.Add(new Option(2, "En progreso"));
            StatusOptions.Add(new Option(3, "Resuelto"));
            StatusOptions.Add(new Option(4, "Cerrado"));

            PriorityOptions.Add(new Option(1, "Baja"));
            PriorityOptions.Add(new Option(2, "Media"));
            PriorityOptions.Add(new Option(3, "Alta"));
            PriorityOptions.Add(new Option(4, "Crítica"));

            CategoryOptions.Add(new Option(1, "Paquete"));
            CategoryOptions.Add(new Option(2, "Entrega"));
            CategoryOptions.Add(new Option(3, "Pago"));
            CategoryOptions.Add(new Option(4, "Otro"));

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SelectedAssignee))
                    AssigneeId = SelectedAssignee?.Id;
            };
        }

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int id;
        [ObservableProperty] private IncidentModel? incident;

        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private string description = string.Empty;
        [ObservableProperty] private int status = 1;
        [ObservableProperty] private int priority = 2;
        [ObservableProperty] private int category = 1;

        public ObservableCollection<UserModel> Users { get; } = new();
        [ObservableProperty] private UserModel? selectedAssignee;
        [ObservableProperty] private string? assigneeId;

        // NUEVO: Propiedad para controlar visibilidad de la sección de asignación
        [ObservableProperty] private bool canAssignResponsible = true;

        public ObservableCollection<Option> StatusOptions { get; } = new();
        public ObservableCollection<Option> PriorityOptions { get; } = new();
        public ObservableCollection<Option> CategoryOptions { get; } = new();

        [ObservableProperty] private Option? selectedPriorityOption;
        [ObservableProperty] private Option? selectedCategoryOption;

        public string EstadoText => MapStatus(Status);
        public string PrioridadText => MapPriority(Priority);
        public string CategoriaText => MapCategory(Category);
        public DateTime Fecha => Incident?.DateTime ?? DateTime.MinValue;
        public string ResponsableText => Incident?.Assignee?.Name
                                         ?? Incident?.Assignee?.Email
                                         ?? Incident?.AssigneeId
                                         ?? "—";
        public string ShipmentCode => Incident?.ShipmentCode ?? "";

        partial void OnStatusChanged(int value) => OnPropertyChanged(nameof(EstadoText));
        partial void OnPriorityChanged(int value) => OnPropertyChanged(nameof(PrioridadText));
        partial void OnCategoryChanged(int value) => OnPropertyChanged(nameof(CategoriaText));
        partial void OnIncidentChanged(IncidentModel? value)
        {
            OnPropertyChanged(nameof(Fecha));
            OnPropertyChanged(nameof(ResponsableText));
            OnPropertyChanged(nameof(ShipmentCode));
        }

        partial void OnSelectedPriorityOptionChanged(Option? value)
        {
            if (value is null) return;
            Priority = value.Id;
            if (Incident is not null) _ = SaveAsync();
        }

        partial void OnSelectedCategoryOptionChanged(Option? value)
        {
            if (value is null) return;
            Category = value.Id;
            if (Incident is not null) _ = SaveAsync();
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // NUEVO: Verificar rol del usuario actual para controlar permisos
                await LoadCurrentUserPermissionsAsync();

                var model = await _incidents.GetByIdAsync(Id);
                if (model is null)
                {
                    await Shell.Current.DisplayAlert("Aviso", "Incidente no encontrado.", "OK");
                    await BackAsync();
                    return;
                }

                Incident = model;

                Title = model.Title ?? "";
                Description = model.Description ?? "";
                Status = model.Status;
                Priority = model.Priority;
                Category = model.Category;
                AssigneeId = model.AssigneeId;

                // Cargar usuarios solo si el usuario puede asignar responsables
                if (CanAssignResponsible)
                {
                    Users.Clear();
                    var all = await _users.GetAllAsync();
                    foreach (var kv in all)
                    {
                        var u = kv.Value;
                        if (string.IsNullOrEmpty(u.Id)) u.Id = kv.Key;

                        // Filtrar solo usuarios con Role = 2 (Trabajadores)
                        if (u.Role == 2)
                        {
                            Users.Add(u);
                        }
                    }

                    SelectedAssignee = Users.FirstOrDefault(u => u.Id == AssigneeId);
                }

                SelectedPriorityOption = PriorityOptions.FirstOrDefault(o => o.Id == Priority);
                SelectedCategoryOption = CategoryOptions.FirstOrDefault(o => o.Id == Category);

                OnPropertyChanged(nameof(EstadoText));
                OnPropertyChanged(nameof(PrioridadText));
                OnPropertyChanged(nameof(CategoriaText));
                OnPropertyChanged(nameof(Fecha));
                OnPropertyChanged(nameof(ResponsableText));
                OnPropertyChanged(nameof(ShipmentCode));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo cargar el incidente: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        // NUEVO MÉTODO: Cargar permisos del usuario actual
        private async Task LoadCurrentUserPermissionsAsync()
        {
            try
            {
                var currentUserId = await SecureStorage.GetAsync("user_id");

                if (!string.IsNullOrWhiteSpace(currentUserId))
                {
                    var currentUser = await _users.GetByIdAsync(currentUserId);
                    var currentUserRole = currentUser?.Role ?? 3;

                    // Solo Admin (1) puede asignar responsables
                    // Trabajadores (2) y usuarios normales (3) no pueden
                    CanAssignResponsible = currentUserRole == 1;

                    System.Diagnostics.Debug.WriteLine($"[IncidentDetailVM] Current user role: {currentUserRole}, Can assign: {CanAssignResponsible}");
                }
                else
                {
                    CanAssignResponsible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentDetailVM] Error loading user permissions: {ex.Message}");
                CanAssignResponsible = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy || Incident is null) return;

            if (string.IsNullOrWhiteSpace(Title))
            {
                await Shell.Current.DisplayAlert("Validación", "El título es requerido.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                UserModel? assignee = null;
                if (!string.IsNullOrWhiteSpace(AssigneeId))
                    assignee = await _users.GetByIdAsync(AssigneeId);

                Incident.Title = Title.Trim();
                Incident.Description = (Description ?? string.Empty).Trim();
                Incident.Status = Status;
                Incident.Priority = Priority;
                Incident.Category = Category;

                // Solo actualizar asignación si el usuario tiene permisos
                if (CanAssignResponsible)
                {
                    Incident.AssigneeId = AssigneeId;
                    Incident.Assignee = assignee;
                }

                if (Incident.Status is 3 or 4 && Incident.ResolvedAt is null)
                    Incident.ResolvedAt = DateTime.UtcNow;

                await _incidents.UpdateAsync(Incident);

                WeakReferenceMessenger.Default.Send(new IncidentSavedMessage(Incident));

            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task SetStatusAsync(object? parameter)
        {
            if (parameter is null) return;

            int newStatus = parameter switch
            {
                int i => i,
                string s when int.TryParse(s, out var v) => v,
                Option opt => opt.Id,
                _ => Status
            };

            Status = newStatus;
            await SaveAsync();
        }

        [RelayCommand]
        private async Task AssignAsync(UserModel? user)
        {
            if (user is null || !CanAssignResponsible) return;
            SelectedAssignee = user;
            await SaveAsync();
        }

        [RelayCommand]
        private async Task GoToShipmentAsync()
        {
            if (string.IsNullOrWhiteSpace(ShipmentCode)) return;
            await Shell.Current.GoToAsync($"{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(ShipmentCode)}");
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (Incident is null) return;
            var ok = await Shell.Current.DisplayAlert("Confirmar", "¿Eliminar incidente?", "Sí", "No");
            if (!ok) return;

            try
            {
                var deletedId = Incident.Id;
                await _incidents.DeleteAsync(deletedId);
                WeakReferenceMessenger.Default.Send(new IncidentDeletedMessage(deletedId));
                await BackAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
            }
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

        private static string MapStatus(int s) => s switch
        {
            1 => "Abierto",
            2 => "En progreso",
            3 => "Resuelto",
            4 => "Cerrado",
            _ => "Desconocido"
        };
        private static string MapPriority(int p) => p switch
        {
            1 => "Baja",
            2 => "Media",
            3 => "Alta",
            4 => "Crítica",
            _ => "N/A"
        };
        private static string MapCategory(int c) => c switch
        {
            1 => "Paquete",
            2 => "Entrega",
            3 => "Pago",
            4 => "Otro",
            _ => "N/A"
        };
    }
}