using Firebase.Auth;
using Firebase.Auth.Providers;
using Microsoft.Extensions.Logging;
using ReportesDePaqueteria.MVVM.Models;
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
                ApiKey = "AIzaSyC0WbYXtxk49ABY3Bb-VX9GFuoZWT9shHU",  
                AuthDomain = "ruby-on-rails-10454.firebaseapp.com", 
                Providers = new FirebaseAuthProvider[]
     {
        new EmailProvider()
     },
            };
            builder.Services.AddSingleton(new FirebaseAuthClient(firebaseConfig));

            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IShipmentRepository, ShipmentRepository>();
            builder.Services.AddSingleton<IIncidentRepository, IncidentRepository>();
            builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();


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

            builder.Services.AddTransient<ShipmentListViewModel>();   
            builder.Services.AddTransient<ShipmentDetailViewModel>(); 
            builder.Services.AddTransient<ShipmentFormViewModel>();   

            builder.Services.AddTransient<ShipmentListPage>();        
            builder.Services.AddTransient<ShipmentDetailPage>();      
            builder.Services.AddTransient<ShipmentFormPage>();

            builder.Services.AddTransient<IncidentListViewModel>();
            builder.Services.AddTransient<IncidentListPage>();

            builder.Services.AddTransient<IncidentFormViewModel>();
            builder.Services.AddTransient<IncidentFormPage>();

            builder.Services.AddTransient<IncidentDetailViewModel>();
            builder.Services.AddTransient<IncidentDetailPage>();

            builder.Services.AddTransient<NotificationViewModel>();
            builder.Services.AddTransient<NotificationsPage>();




            return builder.Build();
        }
    }
}
