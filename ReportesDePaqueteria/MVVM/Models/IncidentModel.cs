using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVVM.Models
{
    public class IncidentModel
    {
        public int Id { get; set; } // Identificador único del incidente
        public string Title { get; set; } // Título del incidente
        public string Description { get; set; } // Descripción detallada del incidente
        public int Status { get; set; } // 1: Open, 2: In Progress, 3: Resolved, 4: Closed Esto puede que ocupe un converter
        public int Priority { get; set; } // 1: Low, 2: Medium, 3: High, 4: Critical Esto tambien puede que ocupe un converter
        public int Category { get; set; } // 1: Problema del paquete, 2: Problema de entrega, 3: Problema de pago, 4: Otro
        public DateTime DateTime { get; set; } // Fecha y hora del incidente
    }
}
