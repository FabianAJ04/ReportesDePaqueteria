using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVVM.Models
{
    public class UserModel
    {
        public int ID { get; set; } // Identificador único del usuario - busquemos generarlo automáticamente
        public string Name { get; set; } // Nombre del usuario
        public string Email { get; set; } // Correo electrónico del usuario, con esto haremos la autenticación
        public string Password { get; set; } // Contraseña del usuario, con esto haremos la autenticación
        public int Role { get; set; } // Rol del usuario: 1: Admin, 2: Trabajador, 3: Usuario normal, 4: <- dejemos espacio para otro rol
    }
}
