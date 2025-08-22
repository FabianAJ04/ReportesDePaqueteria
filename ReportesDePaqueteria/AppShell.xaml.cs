using ReportesDePaqueteria.MVVM.Views;

namespace ReportesDePaqueteria
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ShipmentDetailPage), typeof(ShipmentDetailPage));
            Routing.RegisterRoute(nameof(ShipmentFormPage), typeof(ShipmentFormPage));
            Routing.RegisterRoute(nameof(ShipmentListPage), typeof(ShipmentListPage));

            Routing.RegisterRoute(nameof(IncidentFormPage), typeof(IncidentFormPage));
            Routing.RegisterRoute(nameof(IncidentDetailPage), typeof(IncidentDetailPage));
            Routing.RegisterRoute(nameof(IncidentListPage), typeof(IncidentListPage));

            Routing.RegisterRoute(nameof(UserListPage), typeof(UserListPage));
            Routing.RegisterRoute(nameof(UserProfilePage), typeof(UserProfilePage));
            Routing.RegisterRoute(nameof(ChangePasswordPage), typeof(ChangePasswordPage));

            Routing.RegisterRoute(nameof(ResourceAssignPage), typeof(ResourceAssignPage));

            Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
            



        }
    }
}
