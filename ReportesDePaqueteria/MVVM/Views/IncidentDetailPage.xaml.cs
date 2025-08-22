using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views;

public partial class IncidentDetailPage : ContentPage
{
    private readonly IncidentDetailViewModel _vm;

    public IncidentDetailPage(IncidentDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
