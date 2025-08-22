using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views;

public partial class IncidentFormPage : ContentPage
{
    private readonly IncidentFormViewModel _vm;

    public IncidentFormPage(IncidentFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.PrefillAsync();
    }
}
