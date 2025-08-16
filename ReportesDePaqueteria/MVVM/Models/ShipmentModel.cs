using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVVM.Models
{
    public class ShipmentModel
    {
        public string Code { get; set; } // Código del envío, busquemos generarlo automáticamente y asegurarnos de que sea único -- usemos esto como TrackingNumer
        public UserModel Sender { get; set; } // Los datos del Usuario que generó el envio
        public UserModel Worker { get; set; } // Los datos del Usuario que está trabajando en el envío, busquemos que cuando se asigne un trabajador al envio
        public string ReceiverName { get; set; } // Nombre del Usuario que recibirá el envio, no tiene que estar registrado en la aplicación, solo se pone un nombre
        public int Status { get; set; } // 1: Enviado, 2: En tránsito, 3: Entregado, 4: Cancelado, 5: Ver incidente 
        public DateTime CreatedDate { get; set; } // Fecha y hora en que se creó el envío
        public string Description { get; set; } // Descripción del envío, puede ser un texto libre para que el usuario ponga lo que quiera
        public IncidentModel Incident { get; set; } // Relación con el modelo de incidente, si hay un incidente relacionado con el envío, se guarda aquí
    }
}
