using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Text;

namespace ReportesDePaqueteria.MVVM.Models
{
    public interface INotificationRepository
    {
        Task CreateAsync(NotificationModel n); 
        Task<IReadOnlyList<NotificationModel>> GetLatestForCurrentUserAsync(int take = 50);
        IObservable<FirebaseEvent<NotificationModel>> ObserveCurrentUser();
        Task MarkAsReadAsync(int id);
    }

    public sealed class NotificationRepository : INotificationRepository
    {
        private const string DbUrl = "https://ruby-on-rails-10454-default-rtdb.firebaseio.com/";
        private const string Root = "Notification";

        private readonly FirebaseClient _client;

        public NotificationRepository()
        {
            _client = new FirebaseClient(DbUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await SecureStorage.GetAsync("id_token")
            });

        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var uid = await SecureStorage.GetAsync("user_id");
            if (!string.IsNullOrWhiteSpace(uid)) return uid;

            var token = await SecureStorage.GetAsync("id_token");
            if (string.IsNullOrWhiteSpace(token)) return "";
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return "";
                string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[1])));
                var userId = TryExtract(payloadJson, "\"user_id\":\"", "\"")
                          ?? TryExtract(payloadJson, "\"sub\":\"", "\"");
                return userId ?? "";
            }
            catch { return ""; }

            static string PadBase64(string s)
            {
                int pad = 4 - (s.Length % 4);
                return s + (pad < 4 ? new string('=', pad) : "");
            }
            static string? TryExtract(string text, string start, string end)
            {
                var i = text.IndexOf(start);
                if (i < 0) return null;
                i += start.Length;
                var j = text.IndexOf(end, i);
                if (j < 0) return null;
                return text.Substring(i, j - i);
            }
        }

        private async Task<int> NextIdAsync(string userId)
        {
            var max = 0;

            try
            {
                var snapsK = await _client.Child(Root).Child(userId)
                    .OnceAsync<NotificationModel>().ConfigureAwait(false);
                foreach (var s in snapsK)
                    if (s.Object?.Id > max) max = s.Object.Id;
            }
            catch {}

            try
            {
                var snapsRoot = await _client.Child(Root).Child(userId)
                    .OnceAsync<NotificationModel>().ConfigureAwait(false);
                foreach (var s in snapsRoot)
                {
                    if (int.TryParse(s.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > max) max = id;
                    else if (s.Object?.Id > max) max = s.Object.Id;
                }
            }
            catch {  }

            return max + 1;
        }

        public async Task CreateAsync(NotificationModel n)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId)) return; 

            if (n.Id <= 0) n.Id = await NextIdAsync(userId).ConfigureAwait(false);
            if (n.Timestamp == default) n.Timestamp = DateTime.UtcNow;
            n.RecipientUserId = userId;

            await _client.Child(Root).Child(userId)
                         .PostAsync(n)
                         .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<NotificationModel>> GetLatestForCurrentUserAsync(int take = 50)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<NotificationModel>();

            try
            {
                var snaps = await _client.Child(Root).Child(userId)
                    .OnceAsync<NotificationModel>().ConfigureAwait(false);

                var listK = snaps.Select(s =>
                {
                    var x = s.Object;
                    if (x == null) return null;
                    x.RecipientUserId = userId;
                    return x;
                })
                .Where(x => x != null)
                .OrderByDescending(x => x!.Timestamp)
                .Take(take)
                .ToList()!;

                if (listK.Count > 0) return listK;
            }
            catch {  }

            try
            {
                var snapsOld = await _client.Child(Root).Child(userId)
                    .OnceAsync<NotificationModel>().ConfigureAwait(false);

                var listOld = snapsOld.Select(s =>
                {
                    var x = s.Object;
                    if (x == null) return null;
                    if (x.Id == 0 && int.TryParse(s.Key, out var id)) x.Id = id;
                    x.RecipientUserId = userId;
                    return x;
                })
                .Where(x => x != null)
                .OrderByDescending(x => x!.Timestamp)
                .Take(take)
                .ToList()!;

                return listOld;
            }
            catch
            {
                return Array.Empty<NotificationModel>();
            }
        }

        public IObservable<FirebaseEvent<NotificationModel>> ObserveCurrentUser()
        {
            return System.Reactive.Linq.Observable.Create<FirebaseEvent<NotificationModel>>(async (obs, ct) =>
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    obs.OnCompleted();
                    return System.Reactive.Disposables.Disposable.Empty;
                }

                return _client.Child(Root).Child(userId)
                              .AsObservable<NotificationModel>()
                              .Subscribe(obs);
            });
        }

        public async Task MarkAsReadAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                var snaps = await _client.Child(Root).Child(userId)
                    .OnceAsync<NotificationModel>().ConfigureAwait(false);

                var target = snaps.FirstOrDefault(s => s.Object?.Id == id);
                if (target != null && target.Object is NotificationModel notif && !notif.IsRead)
                {
                    notif.IsRead = true;
                    await _client.Child(Root).Child(userId).Child(target.Key!)
                        .PatchAsync(notif).ConfigureAwait(false);
                    return;
                }
            }
            catch {  }

            var node = _client.Child(Root).Child(userId).Child(id.ToString(CultureInfo.InvariantCulture));
            try
            {
                var notif = await node.OnceSingleAsync<NotificationModel>().ConfigureAwait(false);
                if (notif == null) return;
                if (!notif.IsRead)
                {
                    notif.IsRead = true;
                    await node.PatchAsync(notif).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        public async Task DeleteAllAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                // Delete from new structure (/k/)
                await _client.Child(Root).Child(userId).DeleteAsync();

                // Delete from old structure (direct under userId)
                await _client.Child(Root).Child(userId).DeleteAsync();
            }
            catch { }
        }
    }
}
