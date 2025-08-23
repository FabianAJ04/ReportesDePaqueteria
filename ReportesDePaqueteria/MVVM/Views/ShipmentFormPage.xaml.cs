using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ShipmentFormPage : ContentPage
    {
        public ShipmentFormViewModel VM { get; }

        public ShipmentFormPage(ShipmentFormViewModel vm)
        {
            InitializeComponent();
            VM = vm;                 
            BindingContext = VM;    
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
