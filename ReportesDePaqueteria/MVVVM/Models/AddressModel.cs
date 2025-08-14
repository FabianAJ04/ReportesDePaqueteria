using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVVM.Models
{
    public class AddressModel //Aqui cualquier cosa Copilot me ayudo con los comentarios
    {
        public string Street { get; set; } // Calle de la dirección
        public string City { get; set; } // Tomemos esto como canton
        public string State { get; set; } // Tomemos esto como provincia
        public string PostalCode { get; set; } // Usemos esto con codigos postales de los distritos, ejemplo: En Belen, Heredia es 40701 para identificar San Antonio, 40702 para identificar La Ribera, 40703 para identificar La Asuncion.
        public string Country { get; set; } // Pais de la dirección, por defecto Costa Rica, pero podemos dejarlo como un campo editable para futuras expansiones
        public string AdditionalInfo { get; set; } // Información adicional de la dirección, como referencias o puntos de interés cercanos
    }
}
