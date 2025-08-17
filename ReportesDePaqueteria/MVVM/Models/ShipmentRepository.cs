using Firebase.Database;
using Firebase.Database.Query;
using ReportesDePaqueteria.MVVM.Models;

namespace ReportesDePaqueteria.MVVM.Models
{
    public class ShipmentRepository
    {
        private readonly FirebaseClient _client;
        public ShipmentRepository()
        {
            _client = new FirebaseClient("https://fir-maui-7b446-default-rtdb.firebaseio.com/");
        }

        //1. Crear el documento
        public async Task CreateDocumentAsync(ShipmentModel Shipment)
        {
            await _client.Child("Shipment").PostAsync(Shipment);

            Console.WriteLine($"El envio {Shipment.Code} creado exitosamente!");
        }

        //2. Método GetAll
        public async Task<Dictionary<string, ShipmentModel>> GetAllAsync()
        {
            try
            {
                var lstShipments = await _client.Child("Shipment").OnceAsync<ShipmentModel>();
                var ShipmentDictionary = new Dictionary<string, ShipmentModel>();

                foreach (var Shipment in lstShipments)
                {
                    ShipmentDictionary.Add(Shipment.Key, Shipment.Object);
                }

                return ShipmentDictionary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar los datos: {ex.Message}");
                return new Dictionary<string, ShipmentModel>();
            }
        }

        // 3. Método Update
        public async Task UpdateDocumentAsync(ShipmentModel Shipment, string Key)
        {
            await _client.Child("Shipment").Child(Key).PatchAsync(Shipment);
            Console.WriteLine($"El envio {Shipment.Code} se actualizo exitosamente");
        }

        // 4. Método Delete
        public async Task DeleteDocumentAsync(string Key)
        {
            await _client.Child("Shipment").Child(Key).DeleteAsync();
            Console.WriteLine("El Shipment ha sido eliminado");
        }
    }
}
