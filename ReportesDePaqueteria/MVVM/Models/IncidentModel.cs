namespace ReportesDePaqueteria.MVVM.Models
{
    public class IncidentModel
    {
        public int Id { get; set; }                 // Identificador único del incidente
        public string Title { get; set; } = "";     // Título
        public string Description { get; set; } = "";// Descripción
        public int Status { get; set; } = 1;         // Estados: 1 Open, 2 In Progress, 3 Resolved, 4 Closed

        public int Priority { get; set; } = 2;         // Prioridad: 1 Low, 2 Medium, 3 High, 4 Critical

        public int Category { get; set; } = 1;  // Categoría: 1 Problema del paquete, 2 Problema de entrega, 3 Problema de pago, 4 Otro

        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public string ShipmentCode { get; set; } = ""; // Code del Shipment

        public string? AssigneeId { get; set; }        // id de UserModel para persistir
        public UserModel? Assignee { get; set; }       // datos del asignado (para UI)

        public string? CreatedById { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
