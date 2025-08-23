using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReportesDePaqueteria.MVVM.Models
{
    public interface IIncidentRepository
    {
        Task<int> NextIdAsync();
        Task CreateAsync(IncidentModel incident);
        Task<IncidentModel?> GetByIdAsync(int id);
        Task<IReadOnlyDictionary<int, IncidentModel>> GetAllAsync();
        Task<IReadOnlyList<IncidentModel>> GetByShipmentAsync(string shipmentCode);
        Task UpdateAsync(IncidentModel incident);
        Task DeleteAsync(int id);
        IObservable<FirebaseEvent<IncidentModel>> ObserveAll();
    }

    public sealed class IncidentRepository : IIncidentRepository
    {
        private const string DbUrl = "https://react-firebase-6c246-default-rtdb.firebaseio.com/";
        private const string Node = "Incidents";
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
            i.Title ??= string.Empty;
            i.Description ??= string.Empty;
            i.ShipmentCode ??= string.Empty;
            if (i.DateTime == default)
                i.DateTime = DateTime.UtcNow;
            else if (i.DateTime.Kind == DateTimeKind.Unspecified)
                i.DateTime = DateTime.SpecifyKind(i.DateTime, DateTimeKind.Utc);
            if (i.Status is < 1 or > 4) i.Status = 1;
            if (i.Priority is < 1 or > 4) i.Priority = 2;
            if (i.Category is < 1 or > 4) i.Category = 1;
            if (i.ResolvedAt.HasValue && i.ResolvedAt.Value.Kind == DateTimeKind.Unspecified)
                i.ResolvedAt = DateTime.SpecifyKind(i.ResolvedAt.Value, DateTimeKind.Utc);
            return i;
        }
        public IObservable<FirebaseEvent<IncidentModel>> ObserveAll()
        {
            return _client
                .Child(Node)
                .AsObservable<IncidentModel>();
        }

        public async Task<int> NextIdAsync()
        {
            try
            {
                var snaps = await _client.Child(Node).OnceAsync<IncidentModel>().ConfigureAwait(false);
                var max = 0;
                foreach (var s in snaps)
                {
                    if (int.TryParse(s.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > max)
                        max = id;
                    else if (s.Object?.Id > max)
                        max = s.Object.Id;
                }
                if (max > 0) return max + 1;

                var list = await _client.Child(Node).OnceSingleAsync<List<IncidentModel>>().ConfigureAwait(false);
                if (list != null)
                {
                    foreach (var item in list)
                        if (item != null && item.Id > max) max = item.Id;
                }
                return (max > 0 ? max + 1 : 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentRepo.NextIdAsync] Error: {ex}");
                return 1;
            }
        }

        public async Task CreateAsync(IncidentModel incident)
        {
            if (incident == null) throw new ArgumentNullException(nameof(incident));
            incident = Normalize(incident);

            // 1) Punto de partida: si trae Id <=0, proponemos el siguiente
            var candidate = incident.Id > 0 ? incident.Id : await NextIdAsync().ConfigureAwait(false);

            // 2) Función para verificar existencia real en DB
            async Task<bool> ExistsAsync(int id)
            {
                try
                {
                    var existing = await _client.Child(Node)
                                                .Child(id.ToString(CultureInfo.InvariantCulture))
                                                .OnceSingleAsync<IncidentModel>()
                                                .ConfigureAwait(false);
                    return existing != null;
                }
                catch
                {
                    return false;
                }
            }

            // 3) Busca el siguiente Id libre aunque NextIdAsync se equivoque
            var safety = 0;
            while (await ExistsAsync(candidate).ConfigureAwait(false))
            {
                candidate++;
                if (++safety > 10000) throw new InvalidOperationException("No hay Id libre disponible.");
            }

            incident.Id = candidate;

            await _client.Child(Node)
                         .Child(candidate.ToString(CultureInfo.InvariantCulture))
                         .PutAsync(incident)
                         .ConfigureAwait(false);
        }


        public async Task<IncidentModel?> GetByIdAsync(int id)
        {
            if (id <= 0) return null;

            try
            {
                var obj = await _client.Child(Node)
                                       .Child(id.ToString(CultureInfo.InvariantCulture))
                                       .OnceSingleAsync<IncidentModel>()
                                       .ConfigureAwait(false);
                return obj == null ? null : Normalize(obj);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentRepo.GetByIdAsync] Error: {ex}");
                return null;
            }
        }

        public async Task<IReadOnlyDictionary<int, IncidentModel>> GetAllAsync()
        {
            var dict = new Dictionary<int, IncidentModel>();
            try
            {
                var snaps = await _client.Child(Node).OnceAsync<IncidentModel>().ConfigureAwait(false);
                if (snaps.Count > 0)
                {
                    foreach (var s in snaps)
                    {
                        var m = Normalize(s.Object);
                        if (m.Id == 0 && int.TryParse(s.Key, out var parsed))
                            m.Id = parsed;
                        if (m.Id != 0)
                            dict[m.Id] = m;
                    }
                    System.Diagnostics.Debug.WriteLine($"[IncidentRepo.GetAllAsync] map.Count = {dict.Count}");
                    return dict;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentRepo.GetAllAsync] map read failed: {ex.Message}");
            }

            try
            {
                var list = await _client.Child(Node).OnceSingleAsync<List<IncidentModel>>().ConfigureAwait(false);
                if (list != null)
                {
                    for (int idx = 0; idx < list.Count; idx++)
                    {
                        var item = list[idx];
                        if (item == null) continue;
                        var m = Normalize(item);
                        var id = (m.Id > 0 ? m.Id : idx);
                        if (id != 0)
                            dict[id] = m;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[IncidentRepo.GetAllAsync] array.Count = {dict.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentRepo.GetAllAsync] array read failed: {ex}");
            }

            return dict;
        }

        public async Task<IReadOnlyList<IncidentModel>> GetByShipmentAsync(string shipmentCode)
        {
            shipmentCode ??= string.Empty;
            try
            {
                var all = await GetAllAsync().ConfigureAwait(false);
                return all.Values
                          .Where(m => string.Equals(m.ShipmentCode ?? string.Empty, shipmentCode, StringComparison.OrdinalIgnoreCase))
                          .OrderByDescending(i => i.DateTime)
                          .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentRepo.GetByShipmentAsync] Error: {ex}");
                return Array.Empty<IncidentModel>();
            }
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

        public async Task DebugRawAsync()
        {
            try
            {
                var baseUrl = $"{DbUrl}{Node}.json";
                var token = await SecureStorage.GetAsync("id_token");

                System.Diagnostics.Debug.WriteLine($"[DebugRaw] id_token is {(string.IsNullOrEmpty(token) ? "NULL/empty" : "PRESENT")}");

                using var http = new HttpClient();

                var urlNoAuth = baseUrl;
                var rawNoAuth = await http.GetStringAsync(urlNoAuth);
                System.Diagnostics.Debug.WriteLine($"[DebugRaw] (no auth) {urlNoAuth}");
                System.Diagnostics.Debug.WriteLine($"[DebugRaw] (no auth) {rawNoAuth.Substring(0, Math.Min(500, rawNoAuth.Length))}");

                if (!string.IsNullOrEmpty(token))
                {
                    var urlAuth = baseUrl + $"?auth={token}";
                    var rawAuth = await http.GetStringAsync(urlAuth);
                    System.Diagnostics.Debug.WriteLine($"[DebugRaw] (auth) {urlAuth}");
                    System.Diagnostics.Debug.WriteLine($"[DebugRaw] (auth) {rawAuth.Substring(0, Math.Min(500, rawAuth.Length))}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[DebugRaw] HTTP error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DebugRaw] Error: {ex}");
            }
        }
    }
}
