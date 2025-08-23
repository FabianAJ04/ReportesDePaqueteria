using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class IncidentDetailPage : ContentPage
    {
        public IncidentDetailPage(IncidentDetailViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is IncidentDetailViewModel vm)
                await vm.LoadAsync();
        }
    }
}
