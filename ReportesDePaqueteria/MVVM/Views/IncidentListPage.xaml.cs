using ReportesDePaqueteria.MVVM.ViewModels;
using ReportesDePaqueteria.MVVM.Models; 

namespace ReportesDePaqueteria.MVVM.Views;

public partial class IncidentListPage : ContentPage
{
    private readonly IncidentListViewModel _vm;

    public IncidentListPage(IncidentListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.IsRefreshing = true;
        await _vm.LoadAsync();
    }

    private async void OnVerClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is IncidentModel incident)
            await Shell.Current.DisplayAlert("Incidente", $"Abrir detalle #{incident.Id}", "OK");
    }
}
