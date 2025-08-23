namespace ReportesDePaqueteria.MVVM.Models
{
    public enum NotificationType { ShipmentCreated = 1, IncidentCreated = 2 }

    public class NotificationModel
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }

        public string RecipientUserId { get; set; } = "";  // se completa automáticamente
        public string? ShipmentCode { get; set; }
        public int? IncidentId { get; set; }
        public string? DeepLink { get; set; }
    }
}
