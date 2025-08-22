using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace ReportesDePaqueteria.MVVM.Models
{
    public interface IIncidentRepository
    {
        Task<int> NextIdAsync();                                      // Siguiente Id incremental
        Task CreateAsync(IncidentModel incident);                     // Incidents/{Id}
        Task<IncidentModel?> GetByIdAsync(int id);                    // Lee Incidents/{Id}
        Task<IReadOnlyDictionary<int, IncidentModel>> GetAllAsync();  // Lee todo el nodo
        Task<IReadOnlyList<IncidentModel>> GetByShipmentAsync(string shipmentCode); // Filtra por envío
        Task UpdateAsync(IncidentModel incident);                     // Patch por Id
        Task DeleteAsync(int id);                                     // Borra por Id
    }

    public sealed class IncidentRepository : IIncidentRepository
    {
        private const string DbUrl = "https://react-firebase-6c246-default-rtdb.firebaseio.com/";
        private const string Node = "Incidents"; // unificado
        private readonly FirebaseClient _client;

        public IncidentRepository()
        {
            _client = new FirebaseClient(
                DbUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = async () => await SecureStorage.GetAsync("id_token")
                });
        }

        private static IncidentModel Normalize(IncidentModel i)
        {
            i ??= new IncidentModel();
            i.Title = i.Title ?? string.Empty;
            i.Description = i.Description ?? string.Empty;
            i.ShipmentCode = i.ShipmentCode ?? string.Empty;
            if (i.DateTime == default) i.DateTime = DateTime.UtcNow;

            i.AssigneeId = i.AssigneeId;
            i.Assignee = i.Assignee;
            i.CreatedById = i.CreatedById;
            i.ResolutionNotes = i.ResolutionNotes;
            i.ResolvedAt = i.ResolvedAt;
            return i;
        }

        public async Task<int> NextIdAsync()
        {
            try
            {
                var all = await _client.Child(Node).OnceAsync<IncidentModel>().ConfigureAwait(false);
                var max = 0;
                foreach (var snap in all)
                {
                    if (int.TryParse(snap.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > max)
                        max = id;
                }
                return max + 1;
            }
            catch
            {
                return 1;
            }
        }

        public async Task CreateAsync(IncidentModel incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));

            incident = Normalize(incident);

            if (incident.Id <= 0)
                incident.Id = await NextIdAsync().ConfigureAwait(false);

            await _client.Child(Node)
                         .Child(incident.Id.ToString(CultureInfo.InvariantCulture))
                         .PutAsync(incident)
                         .ConfigureAwait(false);
        }

        public async Task<IncidentModel?> GetByIdAsync(int id)
        {
            if (id <= 0) return null;

            var obj = await _client.Child(Node)
                                   .Child(id.ToString(CultureInfo.InvariantCulture))
                                   .OnceSingleAsync<IncidentModel>()
                                   .ConfigureAwait(false);

            return obj == null ? null : Normalize(obj);
        }

        public async Task<IReadOnlyDictionary<int, IncidentModel>> GetAllAsync()
        {
            var dict = new Dictionary<int, IncidentModel>();
            try
            {
                var snaps = await _client.Child(Node).OnceAsync<IncidentModel>().ConfigureAwait(false);
                foreach (var snap in snaps)
                {
                    var model = Normalize(snap.Object);
                    if (model.Id == 0 && int.TryParse(snap.Key, out var parsed))
                        model.Id = parsed; 

                    if (model.Id != 0)
                        dict[model.Id] = model;
                }
            }
            catch
            {
            }
            return dict;
        }

        public async Task<IReadOnlyList<IncidentModel>> GetByShipmentAsync(string shipmentCode)
        {
            shipmentCode ??= string.Empty;
            var result = new List<IncidentModel>();

            try
            {
                var snaps = await _client.Child(Node).OnceAsync<IncidentModel>().ConfigureAwait(false);
                foreach (var snap in snaps)
                {
                    var model = Normalize(snap.Object);
                    if (model.Id == 0 && int.TryParse(snap.Key, out var parsed))
                        model.Id = parsed;

                    if (string.Equals(model.ShipmentCode, shipmentCode, StringComparison.OrdinalIgnoreCase))
                        result.Add(model);
                }
            }
            catch
            {
            }

            return result.OrderByDescending(i => i.DateTime).ToList();
        }

        public async Task UpdateAsync(IncidentModel incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));
            if (incident.Id <= 0) throw new ArgumentException("Incident.Id es requerido (> 0).");

            incident = Normalize(incident);

            await _client.Child(Node)
                         .Child(incident.Id.ToString(CultureInfo.InvariantCulture))
                         .PatchAsync(incident)
                         .ConfigureAwait(false);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0) return;

            await _client.Child(Node)
                         .Child(id.ToString(CultureInfo.InvariantCulture))
                         .DeleteAsync()
                         .ConfigureAwait(false);
        }
    }
}
