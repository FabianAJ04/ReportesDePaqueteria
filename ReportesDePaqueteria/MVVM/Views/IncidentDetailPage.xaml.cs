using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views;

public partial class IncidentDetailPage : ContentPage
{
    public IncidentDetailPage(IncidentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IncidentDetailViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}