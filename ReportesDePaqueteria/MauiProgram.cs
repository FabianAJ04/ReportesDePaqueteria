using Firebase.Auth;
using Firebase.Auth.Providers;
using Microsoft.Extensions.Logging;
using ReportesDePaqueteria.MVVM.ViewModels;
using ReportesDePaqueteria.MVVM.Views;

namespace ReportesDePaqueteria
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                // .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var firebaseConfig = new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyCb7zzUVwbLrlSi1R8V9gUeO49_NmZmbWo",  
                AuthDomain = "react-firebase-6c246.firebaseapp.com", 
                Providers = new FirebaseAuthProvider[]
     {
        new EmailProvider()
     },
            };
            builder.Services.AddSingleton(new FirebaseAuthClient(firebaseConfig));

            builder.Services.AddSingleton<IUserRepository, UserRepository>();

            // ViewModels
            builder.Services.AddTransient<SignInViewModel>();
            builder.Services.AddTransient<SignUpViewModel>();

            builder.Services.AddTransient<SignInView>();
            builder.Services.AddTransient<SignUpView>();

            builder.Services.AddTransient<HomePageViewModel>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<UserListPage>();
            builder.Services.AddTransient<UserListViewModel>();
            builder.Services.AddTransient<UserListPage>();

            builder.Services.AddTransient<UserProfileViewModel>();
            builder.Services.AddTransient<UserProfilePage>();




            return builder.Build();
        }
    }
}
