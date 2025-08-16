using Firebase.Auth;

namespace ReportesDePaqueteria
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
        public static FirebaseAuthClient AuthClient { get; private set; }
    }
}
