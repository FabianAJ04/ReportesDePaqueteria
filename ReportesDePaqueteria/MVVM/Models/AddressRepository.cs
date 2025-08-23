using Firebase.Database;
using Firebase.Database.Query;
using ReportesDePaqueteria.MVVM.Models;

namespace ReportesDePaqueteria.MVVM.Models
{
    public class AddressRepository
    {
        private readonly FirebaseClient _client;
        public AddressRepository()
        {
            _client = new FirebaseClient("https://ruby-on-rails-10454-default-rtdb.firebaseio.com/");
        }

        //1. Crear el documento
        public async Task CreateDocumentAsync(AddressModel Address)
        {
            await _client.Child("Address").PostAsync(Address);

            Console.WriteLine($"La direccion {Address.Code} creado exitosamente!");
        }

        //2. Método GetAll
        public async Task<Dictionary<string, AddressModel>> GetAllAsync()
        {
            try
            {
                var lstAddresss = await _client.Child("Address").OnceAsync<AddressModel>();
                var AddressDictionary = new Dictionary<string, AddressModel>();

                foreach (var Address in lstAddresss)
                {
                    AddressDictionary.Add(Address.Key, Address.Object);
                }

                return AddressDictionary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar los datos: {ex.Message}");
                return new Dictionary<string, AddressModel>();
            }
        }

        // 3. Método Update
        public async Task UpdateDocumentAsync(AddressModel Address, string Key)
        {
            await _client.Child("Address").Child(Key).PatchAsync(Address);
            Console.WriteLine($"La direccion {Address.Code} se actualizo exitosamente");
        }

        // 4. Método Delete
        public async Task DeleteDocumentAsync(string Key)
        {
            await _client.Child("Address").Child(Key).DeleteAsync();
            Console.WriteLine("El Address ha sido eliminado");
        }
    }
}

