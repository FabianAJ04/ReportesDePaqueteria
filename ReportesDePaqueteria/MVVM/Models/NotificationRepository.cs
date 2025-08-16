using Firebase.Database;
using Firebase.Database.Query;

namespace ReportesDePaqueteria.MVVM.Models
{
    public class NotificationRepository
    {
        private readonly FirebaseClient _client;
        public NotificationRepository()
        {
            _client = new FirebaseClient("https://fir-maui-7b446-default-rtdb.firebaseio.com/");
        }

        //1. Crear el documento
        public async Task CreateDocumentAsync(NotificationModel Notification)
        {
            await _client.Child("Notification").PostAsync(Notification);

            Console.WriteLine($"El envio {Notification.Id} creado exitosamente!");
        }

        //2. Método GetAll
        public async Task<Dictionary<string, NotificationModel>> GetAllAsync()
        {
            try
            {
                var lstNotifications = await _client.Child("Notification").OnceAsync<NotificationModel>();
                var NotificationDictionary = new Dictionary<string, NotificationModel>();

                foreach (var Notification in lstNotifications)
                {
                    NotificationDictionary.Add(Notification.Key, Notification.Object);
                }

                return NotificationDictionary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar los datos: {ex.Message}");
                return new Dictionary<string, NotificationModel>();
            }
        }

        // 3. Método Update
        public async Task UpdateDocumentAsync(NotificationModel Notification, string Key)
        {
            await _client.Child("Notification").Child(Key).PatchAsync(Notification);
            Console.WriteLine($"El envio {Notification.Id} se actualizo exitosamente");
        }

        // 4. Método Delete
        public async Task DeleteDocumentAsync(string Key)
        {
            await _client.Child("Notification").Child(Key).DeleteAsync();
            Console.WriteLine("El Notification ha sido eliminado");
        }
    }
}
