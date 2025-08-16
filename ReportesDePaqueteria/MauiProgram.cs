using Firebase.Auth;
using Firebase.Auth.Providers;
using Microsoft.Extensions.Logging;
using ReportesDePaqueteria.MVVVM.ViewModels;
using ReportesDePaqueteria.MVVVM.Views;

namespace ReportesDePaqueteria
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


#if DEBUG
    		builder.Logging.AddDebug();

            //Conexion con firebaseAuth
            builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig()
            {
                ApiKey = "AIzaSyC0WbYXtxk49ABY3Bb-VX9GFuoZWT9shHU",
                AuthDomain = "ruby-on-rails-10454.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
    {
                    new EmailProvider()
    }
            }));

#endif
            builder.Services.AddSingleton<SignInView>();
            builder.Services.AddSingleton<SignInViewModel>();
            builder.Services.AddSingleton<SignUpView>();
            builder.Services.AddSingleton<SignUpViewModel>();

            return builder.Build();
        }
    }
}
