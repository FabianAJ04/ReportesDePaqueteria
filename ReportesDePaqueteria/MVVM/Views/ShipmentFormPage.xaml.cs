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

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (BindingContext is ShipmentFormViewModel vm)
                await vm.CreateCommand.ExecuteAsync(null);
        }


        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
