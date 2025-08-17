using ReportesDePaqueteria.MVVM.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public class NotificationViewModel : INotifyPropertyChanged
    {
        private readonly NotificationRepository _repository; // Repositorio para manejar las notificaciones
        private Dictionary<string, NotificationModel> _allNotifications; // Almacena todas las notificaciones con sus claves
        private ObservableCollection<NotificationModel> _notifications; // Colección observable para la interfaz de usuario
        private string _search = string.Empty; // Término de búsqueda
        private string _StatusSelected = "Todos"; // Estado seleccionado para filtrar
        private string _TypeSelected = "Todos"; // Tipo seleccionado para filtrar


        public NotificationViewModel()
        {
            _repository = new NotificationRepository();
            _allNotifications = new Dictionary<string, NotificationModel>();
            _notifications = new ObservableCollection<NotificationModel>();

            //Filtros
            status = new ObservableCollection<string>
            {
                "Todos",
                "Leídas",
                "No leídas"
            };
            types = new ObservableCollection<string>
            {
                "Todos",
                "Incidente",
                "Entrega",
                "Otro"
            };

            LoadNotificationsAsync();
        }

        public class NotificationDisplayModel : INotifyPropertyChanged
        {
            private readonly NotificationModel _model;
            private readonly string _key;
            private bool _isRead;

            public NotificationDisplayModel(NotificationModel model, string key)
            {
                _model = model; 
                _key = key; 
                _isRead = false;
            }


            public string Key => _key;
            public NotificationModel Model => _model;

            //xaml binding
            public string Title => _model.Title;
            public string Message => _model.Message;
            public int Priority => _model.Priority;
            public DateTime Timestamp => _model.Timestamp;
            public ShipmentModel Shipment => _model.Shipment;
            public string type
            {
                get
                {
                    return _model.Priority switch
                    {
                        3 => "Incidente",
                        2 => "Entrega",
                        1 => "Otro",
                        _ => "Otro"
                    };
                }
            }
            public string Icon
            {
                get
                {
                    return _model.Priority switch
                    {
                        3 => "alert_circle",
                        2 => "check_circle",
                        1 => "information_circle",
                        _ => "information_circle"
                    };
                }
            }
            public bool IsRead
            {
                get => _isRead;
                set
                {
                    if (_isRead != value)
                    {
                        _isRead = value;
                        OnPropertyChanged(nameof(IsRead));
                    }
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public ObservableCollection<NotificationModel> Notifications
        {
            get => _notifications;
            set
            {
                _notifications = value;
                OnPropertyChanged(nameof(Notifications));
            }
        }
        public string Search
        {
            get => _search;
            set
            {
                if (_search != value)
                {
                    _search = value;
                    OnPropertyChanged(nameof(Search));
                    ApplyFilters();
                }
            }
        }
        public ObservableCollection<string> status { get; set; }
        public string StatusSelected
        {
            get => _StatusSelected;
            set
            {
                if (_StatusSelected != value)
                {
                    _StatusSelected = value;
                    OnPropertyChanged(nameof(StatusSelected));
                    ApplyFilters();
                }
            }
        }
        public ObservableCollection<string> types { get; set; }
        public string TypeSelected
        {
            get => _TypeSelected;
            set
            {
                if (_TypeSelected != value)
                {
                    _TypeSelected = value;
                    OnPropertyChanged(nameof(TypeSelected));
                    ApplyFilters();
                }
            }
        }
        public async Task LoadNotificationsAsync()
        {
            try
            {
                _allNotifications = await _repository.GetAllAsync();
                Notifications.Clear();
                foreach (var kvp in _allNotifications)
                {
                    var notification = kvp.Value;
                    var displayModel = new NotificationDisplayModel(notification, kvp.Key);
                    Notifications.Add(displayModel.Model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar las notificaciones: {ex.Message}");
            }
        }
        public void ApplyFilters()
        {
            var filteredNotifications = _allNotifications.Select(kvp => new NotificationDisplayModel(kvp.Value, kvp.Key))
                .Where(n => FilterBySearch(n) && FilterByEstado(n) && FilterByTipo(n))
                .OrderByDescending(n => n.Timestamp)
                .ToList();
        }
        private bool FilterBySearch(NotificationDisplayModel notification)
        {
            if (string.IsNullOrWhiteSpace(Search))
                return true;

            return notification.Title.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                   notification.Message.Contains(Search, StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterByEstado(NotificationDisplayModel notification)
        {
            return StatusSelected switch
            {
                "Leídas" => notification.IsRead,
                "No leídas" => !notification.IsRead,
                _ => true
            };
        }

        private bool FilterByTipo(NotificationDisplayModel notification)
        {
            return TypeSelected == "Todos" || notification.type == TypeSelected;
        }

        public void MarkAllAsRead()
        {
            foreach (var notification in Notifications)
            {
                notification.IsRead = true;
            }
        }

        public void ClearAll()
        {
            Notifications.Clear();
        }
        public void MarkAsRead(NotificationDisplayModel notification)
        {
            if (notification != null)
            {
                notification.IsRead = true;
                OnPropertyChanged(nameof(Notifications));
            }
        }
        public async Task DeleteNotificationAsync(NotificationDisplayModel notification)
        {
            try
            {
                await _repository.DeleteDocumentAsync(notification.Key);
                Notifications.Remove(notification.Model);
                if(_allNotifications.ContainsKey(notification.Key))
                {
                    _allNotifications.Remove(notification.Key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar la notificación: {ex.Message}");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }    
}

