using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Models;

namespace ReportesDePaqueteria.MVVM.Models
{
    public class IncidentRepository
    {
        private readonly FirebaseClient _client;
        public IncidentRepository()
        {
            _client = new FirebaseClient("https://fir-maui-7b446-default-rtdb.firebaseio.com/");
        }

        //1. Crear el documento
        public async Task CreateDocumentAsync(IncidentModel Incident)
        {
            await _client.Child("Incident").PostAsync(Incident);

            Console.WriteLine($"El incidente {Incident.Id} creado exitosamente!");
        }

        //2. Método GetAll
        public async Task<Dictionary<string, IncidentModel>> GetAllAsync()
        {
            try
            {
                var lstIncidents = await _client.Child("Incident").OnceAsync<IncidentModel>();
                var IncidentDictionary = new Dictionary<string, IncidentModel>();

                foreach (var Incident in lstIncidents)
                {
                    IncidentDictionary.Add(Incident.Key, Incident.Object);
                }

                return IncidentDictionary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar los datos: {ex.Message}");
                return new Dictionary<string, IncidentModel>();
            }
        }

        // 3. Método Update
        public async Task UpdateDocumentAsync(IncidentModel Incident, string Key)
        {
            await _client.Child("Incident").Child(Key).PatchAsync(Incident);
            Console.WriteLine($"El incidente {Incident.Id} se actualizo exitosamente");
        }

        // 4. Método Delete
        public async Task DeleteDocumentAsync(string Key)
        {
            await _client.Child("Incident").Child(Key).DeleteAsync();
            Console.WriteLine("El Incident ha sido eliminado");
        }
    }
}
