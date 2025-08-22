using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Storage;              
using ReportesDePaqueteria.MVVM.Models;

namespace ReportesDePaqueteria.MVVM.Models
{

    public interface IShipmentRepository
    {
        Task CreateAsync(ShipmentModel shipment);                  // Shipments/{Code}
        Task<ShipmentModel?> GetByCodeAsync(string code);          // Lee Shipments/{Code}
        Task<IReadOnlyDictionary<string, ShipmentModel>> GetAllAsync();
        Task UpdateAsync(ShipmentModel shipment);                  // Patch por Code
        Task DeleteAsync(string code);

    }
    public class ShipmentRepository : IShipmentRepository
    {
        private const string DbUrl = "https://react-firebase-6c246-default-rtdb.firebaseio.com/";
        private const string Node = "Shipments";
        private readonly FirebaseClient _client;

        public ShipmentRepository()
        {
            _client = new FirebaseClient(
                DbUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = async () => await SecureStorage.GetAsync("id_token")
                });
        }

        private static ShipmentModel Normalize(ShipmentModel s)
        {
            s ??= new ShipmentModel();
            s.Code = s.Code ?? string.Empty;
            s.ReceiverName = s.ReceiverName ?? string.Empty;
            s.Description = s.Description ?? string.Empty;
            s.Origin ??= string.Empty;
            s.Destination ??= string.Empty;
            if (s.CreatedDate == default) s.CreatedDate = DateTime.UtcNow;
            if (s.Status == 0) s.Status = 1; // 1: Enviado
            return s;
        }

        public async Task CreateAsync(ShipmentModel shipment)
        {
            if (shipment == null) throw new ArgumentNullException(nameof(shipment));
            if (string.IsNullOrWhiteSpace(shipment.Code))
                throw new ArgumentException("Shipment.Code es requerido.");

            shipment = Normalize(shipment);

            await _client.Child(Node)
                         .Child(shipment.Code)
                         .PutAsync(shipment)
                         .ConfigureAwait(false);
        }

        public async Task<ShipmentModel?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var s = await _client.Child(Node)
                                 .Child(code)
                                 .OnceSingleAsync<ShipmentModel>()
                                 .ConfigureAwait(false);
            return s == null ? null : Normalize(s);
        }

        public async Task<IReadOnlyDictionary<string, ShipmentModel>> GetAllAsync()
        {
            var dict = new Dictionary<string, ShipmentModel>();

            try
            {
                var items = await _client.Child(Node).OnceAsync<ShipmentModel>().ConfigureAwait(false);
                foreach (var snap in items)
                {
                    var s = Normalize(snap.Object);
                    if (string.IsNullOrWhiteSpace(s.Code))
                        s.Code = snap.Key; 
                    dict[s.Code] = s;
                }
            }
            catch
            {
            }

            return dict;
        }

        public async Task UpdateAsync(ShipmentModel shipment)
        {
            if (shipment == null) throw new ArgumentNullException(nameof(shipment));
            if (string.IsNullOrWhiteSpace(shipment.Code))
                throw new ArgumentException("Shipment.Code es requerido.");

            await _client.Child(Node)
                         .Child(shipment.Code)
                         .PatchAsync(shipment)
                         .ConfigureAwait(false);
        }

        public async Task DeleteAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            await _client.Child(Node)
                         .Child(code)
                         .DeleteAsync()
                         .ConfigureAwait(false);
        }
    }
}
