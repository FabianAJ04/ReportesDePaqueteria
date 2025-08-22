using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReportesDePaqueteria.MVVM.Models;
using System.Collections.ObjectModel;

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

            // Opciones para pickers
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

            // Mantén AssigneeId sincronizado con SelectedAssignee
            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SelectedAssignee))
                    AssigneeId = SelectedAssignee?.Id;
            };
        }

        // --------- Estado base ----------
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int id;                    // Query param
        [ObservableProperty] private IncidentModel? incident;

        // Campos editables (se copian desde Incident al cargar)
        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private string description = string.Empty;
        [ObservableProperty] private int status = 1;
        [ObservableProperty] private int priority = 2;
        [ObservableProperty] private int category = 1;

        // Asignación
        public ObservableCollection<UserModel> Users { get; } = new();
        [ObservableProperty] private UserModel? selectedAssignee;
        [ObservableProperty] private string? assigneeId;

        // Pickers
        public ObservableCollection<Option> StatusOptions { get; } = new();
        public ObservableCollection<Option> PriorityOptions { get; } = new();
        public ObservableCollection<Option> CategoryOptions { get; } = new();

        // Proyecciones legibles para UI (solo lectura)
        public string EstadoText => MapStatus(Status);
        public string PrioridadText => MapPriority(Priority);
        public string CategoriaText => MapCategory(Category);
        public DateTime Fecha => Incident?.DateTime ?? DateTime.MinValue;
        public string ResponsableText => Incident?.Assignee?.Name
                                         ?? Incident?.Assignee?.Email
                                         ?? Incident?.AssigneeId
                                         ?? "—";
        public string ShipmentCode => Incident?.ShipmentCode ?? "";

        // Refresca proyecciones cuando cambian campos base
        partial void OnStatusChanged(int value)
        {
            OnPropertyChanged(nameof(EstadoText));
        }
        partial void OnPriorityChanged(int value)
        {
            OnPropertyChanged(nameof(PrioridadText));
        }
        partial void OnCategoryChanged(int value)
        {
            OnPropertyChanged(nameof(CategoriaText));
        }
        partial void OnIncidentChanged(IncidentModel? value)
        {
            OnPropertyChanged(nameof(Fecha));
            OnPropertyChanged(nameof(ResponsableText));
            OnPropertyChanged(nameof(ShipmentCode));
        }

        // ========= Comandos =========

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Carga incidente
                var model = await _incidents.GetByIdAsync(Id);
                if (model is null)
                {
                    await Shell.Current.DisplayAlert("Aviso", "Incidente no encontrado.", "OK");
                    await BackAsync();
                    return;
                }

                Incident = model;

                // Copia a campos editables
                Title = model.Title ?? "";
                Description = model.Description ?? "";
                Status = model.Status;
                Priority = model.Priority;
                Category = model.Category;
                AssigneeId = model.AssigneeId;

                // Carga usuarios para asignar
                Users.Clear();
                var all = await _users.GetAllAsync(); // asumiendo Dictionary<string, UserModel>
                foreach (var kv in all)
                {
                    var u = kv.Value;
                    if (string.IsNullOrEmpty(u.Id)) u.Id = kv.Key; // fallback
                    Users.Add(u);
                }

                // Selección del asignado actual
                SelectedAssignee = Users.FirstOrDefault(u => u.Id == AssigneeId);

                // Notifica proyecciones
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

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy) return;
            if (Incident is null) return;

            // Validación mínima
            if (string.IsNullOrWhiteSpace(Title))
            {
                await Shell.Current.DisplayAlert("Validación", "El título es requerido.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                // (Opcional) hidratar Assignee para UI
                UserModel? assignee = null;
                if (!string.IsNullOrWhiteSpace(AssigneeId))
                    assignee = await _users.GetByIdAsync(AssigneeId);

                // Actualiza el modelo
                Incident.Title = Title.Trim();
                Incident.Description = (Description ?? string.Empty).Trim();
                Incident.Status = Status;
                Incident.Priority = Priority;
                Incident.Category = Category;
                Incident.AssigneeId = AssigneeId;
                Incident.Assignee = assignee;

                // Si marcaste como Resuelto o Cerrado, puedes sellar fecha
                if (Incident.Status is 3 or 4 && Incident.ResolvedAt is null)
                    Incident.ResolvedAt = DateTime.UtcNow;

                await _incidents.UpdateAsync(Incident);

                await Shell.Current.DisplayAlert("Éxito", "Incidente actualizado.", "OK");
                await BackAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task SetStatusAsync(int newStatus)
        {
            Status = newStatus;
            await SaveAsync();
        }

        [RelayCommand]
        private async Task AssignAsync(UserModel? user)
        {
            if (user is null) return;
            SelectedAssignee = user; // sincroniza AssigneeId por PropertyChanged
            await SaveAsync();
        }

        [RelayCommand]
        private async Task GoToShipmentAsync()
        {
            if (string.IsNullOrWhiteSpace(ShipmentCode)) return;
            await Shell.Current.GoToAsync($"{{nameof(ShipmentDetailPage)}}?code={Uri.EscapeDataString(ShipmentCode)}");
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (Incident is null) return;
            var ok = await Shell.Current.DisplayAlert("Confirmar", "¿Eliminar incidente?", "Sí", "No");
            if (!ok) return;

            try
            {
                await _incidents.DeleteAsync(Incident.Id);
                await Shell.Current.DisplayAlert("Éxito", "Incidente eliminado.", "OK");
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

        // ========= Helpers =========
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
