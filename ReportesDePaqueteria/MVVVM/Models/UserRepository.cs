using Firebase.Database;
using Firebase.Database.Query;
using ReportesDePaqueteria.MVVVM.Models;

public class UserRepository
{
    private readonly FirebaseClient _firebaseClient;

    public UserRepository()
    {
        _firebaseClient = new FirebaseClient("https://ruby-on-rails-10454-default-rtdb.firebaseio.com/");
    }

    public async Task GuardarUsuario(UserModel user)
    {
        await _firebaseClient
            .Child("Usuarios")
            .Child(user.ID)
            .PutAsync(user);
    }

    public async Task<UserModel?> ObtenerUsuarioPorId(string ID)
    {
        return await _firebaseClient
            .Child("Usuarios")
            .Child(ID)
            .OnceSingleAsync<UserModel>();
    }

    public async Task<List<UserModel>> ObtenerTodos()
    {
        var usuarios = await _firebaseClient
            .Child("Usuarios")
            .OnceAsync<UserModel>();

        return usuarios.Select(u => u.Object).ToList();
    }
}
