using ReportesDePaqueteria.MVVVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVM.Models
{
    public class NotificationModel
    {
        public int Id { get; set; } // Identificador único de la notificación.
        public string Title { get; set; } // Título de la notificación.
        public string Message { get; set; } // Mensaje corto de la notificación.
        public int Priority { get; set; } // Prioridad de la notificación, 1: Low, 2: Medium, 3: High.
        public DateTime Timestamp { get; set; } // Fecha y hora de la notificación.
        public ShipmentModel Shipment { get; set; } //Shipment asociado con la notificación. Este modelo ya contiene información sobre del envio, y los usuarios a ser notificados.
    }
}
