using System;
using Microsoft.Maui.Controls;
using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class IncidentListPage : ContentPage
    {
        public IncidentListPage(IncidentListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is IncidentListViewModel vm && vm.Incidentes.Count == 0 && !vm.IsBusy)
                await vm.LoadAsync();
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//homepage");
        }
    }
}
