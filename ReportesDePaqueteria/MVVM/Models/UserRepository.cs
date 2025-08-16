using Firebase.Database;
using Firebase.Database.Query;
using ReportesDePaqueteria.MVVVM.Models;

public class UserRepository
{
    private readonly FirebaseClient _client;
    public UserRepository()
    {
        _client = new FirebaseClient("https://fir-maui-7b446-default-rtdb.firebaseio.com/");
    }

    //1. Crear el documento
    public async Task CreateDocumentAsync(UserModel User)
    {
        await _client.Child("User").PostAsync(User);

        Console.WriteLine($"El usuario {User.Name} creado exitosamente!");
    }

    //2. Método GetAll
    public async Task<Dictionary<string, UserModel>> GetAllAsync()
    {
        try
        {
            var lstUsers = await _client.Child("User").OnceAsync<UserModel>();
            var UserDictionary = new Dictionary<string, UserModel>();

            foreach (var User in lstUsers)
            {
                UserDictionary.Add(User.Key, User.Object);
            }

            return UserDictionary;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los datos: {ex.Message}");
            return new Dictionary<string, UserModel>();
        }
    }


    public async Task<UserModel?> GetUserById(string ID)
    {
        return await _client
            .Child("Usuarios")
            .Child(ID)
            .OnceSingleAsync<UserModel>();
    }

    public async Task<List<UserModel>> GetAllUsers()
    {
        var usuarios = await _client
            .Child("Usuarios")
            .OnceAsync<UserModel>();

        return usuarios.Select(u => u.Object).ToList();
    }

    // 3. Método Update
    public async Task UpdateDocumentAsync(UserModel User, string Key)
    {
        await _client.Child("User").Child(Key).PatchAsync(User);
        Console.WriteLine($"El usuario {User.Name} se actualizo exitosamente");
    }

    // 4. Método Delete
    public async Task DeleteDocumentAsync(string Key)
    {
        await _client.Child("User").Child(Key).DeleteAsync();
        Console.WriteLine("El User ha sido eliminado");
    }
}

