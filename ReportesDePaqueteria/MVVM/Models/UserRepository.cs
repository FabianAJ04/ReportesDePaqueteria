using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Storage; // SecureStorage
using ReportesDePaqueteria.MVVM.Models;

public interface IUserRepository
{
    Task CreateAsync(UserModel user);                          // Crea/sobrescribe Usuarios/{uid}
    Task<UserModel?> GetByIdAsync(string uid);                 // Lee Usuarios/{uid}
    Task<IReadOnlyDictionary<string, UserModel>> GetAllAsync();// Lee todo el nodo (merge Usuarios + User)
    Task UpdateAsync(UserModel user);                          // Patch en Usuarios/{uid}
    Task DeleteAsync(string uid);                              // Borra Usuarios/{uid} y User/{uid}

    Task CreateDocumentAsync(UserModel user);
}

public class UserRepository : IUserRepository
{
    private const string DatabaseUrl = "https://react-firebase-6c246-default-rtdb.firebaseio.com/";
    private const string NodeUsuarios = "Usuarios";
    private const string NodeLegacy = "User";
    private readonly FirebaseClient _client;

    public UserRepository()
    {
        _client = new FirebaseClient(
            DatabaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await SecureStorage.GetAsync("id_token")
            });
    }

    public UserRepository(FirebaseClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    private static UserModel Map(UserModel? u, string? fallbackKey = null)
    {
        u ??= new UserModel();
        u.Id = string.IsNullOrWhiteSpace(u.Id) ? (fallbackKey ?? string.Empty) : u.Id;
        u.Name = u.Name ?? string.Empty;
        u.Email = (u.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (u.Role == 0) u.Role = 3; 
        return u;
    }

    public async Task CreateAsync(UserModel user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(user.Id)) throw new ArgumentException("User.Id (UID) es requerido.");

        await _client
            .Child(NodeUsuarios)
            .Child(user.Id)
            .PutAsync(user)
            .ConfigureAwait(false);

        System.Diagnostics.Debug.WriteLine($"[Repo] Upsert {NodeUsuarios}/{user.Id}");
    }

    public async Task<UserModel?> GetByIdAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;

        try
        {
            var a = await _client
                .Child(NodeUsuarios)
                .Child(uid)
                .OnceSingleAsync<UserModel>()
                .ConfigureAwait(false);

            if (a != null) return Map(a, uid);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Repo] GetById Usuarios err: {ex.Message}");
        }

        try
        {
            var b = await _client
                .Child(NodeLegacy)
                .Child(uid)
                .OnceSingleAsync<UserModel>()
                .ConfigureAwait(false);

            if (b != null) return Map(b, uid);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Repo] GetById User err: {ex.Message}");
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, UserModel>> GetAllAsync()
    {
        var merged = new Dictionary<string, UserModel>();

        // 1) Lee Usuarios
        try
        {
            var listA = await _client.Child(NodeUsuarios).OnceAsync<UserModel>().ConfigureAwait(false);
            foreach (var snap in listA)
            {
                var user = Map(snap.Object, snap.Key);
                if (!string.IsNullOrEmpty(user.Id))
                    merged[user.Id] = user;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Repo] GetAll Usuarios err: {ex.Message}");
        }

        try
        {
            var listB = await _client.Child(NodeLegacy).OnceAsync<UserModel>().ConfigureAwait(false);
            foreach (var snap in listB)
            {
                var user = Map(snap.Object, snap.Key);
                if (!string.IsNullOrEmpty(user.Id))
                    merged[user.Id] = user; 
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Repo] GetAll User err: {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine($"[Repo] GetAll merged count = {merged.Count}");
        return merged;
    }

    public async Task UpdateAsync(UserModel user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(user.Id)) throw new ArgumentException("User.Id (UID) es requerido.");

        await _client
            .Child(NodeUsuarios)
            .Child(user.Id)
            .PatchAsync(user)
            .ConfigureAwait(false);

        System.Diagnostics.Debug.WriteLine($"[Repo] Patch {NodeUsuarios}/{user.Id}");
    }

    public async Task DeleteAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid)) return;

        try
        {
            await _client.Child(NodeUsuarios).Child(uid).DeleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Repo] Delete Usuarios err: {ex.Message}");
        }

        try
        {
            await _client.Child(NodeLegacy).Child(uid).DeleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Repo] Delete User err: {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine($"[Repo] Delete {uid} complete.");
    }

    public Task CreateDocumentAsync(UserModel user) => CreateAsync(user);
}
