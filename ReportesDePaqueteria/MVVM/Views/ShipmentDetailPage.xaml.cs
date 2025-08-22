using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ShipmentDetailPage : ContentPage, IQueryAttributable
    {
        private readonly ShipmentDetailViewModel _vm;

        public ShipmentDetailPage(ShipmentDetailViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("code", out var raw) && raw is string code)
                _vm.Code = Uri.UnescapeDataString(code);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }

        private void OnVerEtiquetaClicked(object sender, EventArgs e)
        {
            if (_vm.ShowLabelCommand.CanExecute(null))
                _vm.ShowLabelCommand.Execute(null);
        }

        private void OnReportarIncidenciaClicked(object sender, EventArgs e)
        {
            if (_vm.ReportIncidentCommand.CanExecute(null))
                _vm.ReportIncidentCommand.Execute(null);
        }
    }
}
