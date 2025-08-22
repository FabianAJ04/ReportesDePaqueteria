using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using System.Collections.ObjectModel;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentListViewModel : ObservableObject
    {
        private readonly IShipmentRepository _repo;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? search;
        [ObservableProperty] private int statusFilter; // 0=todos, 1 Enviado, 2 En tránsito, 3 Entregado, 4 Cancelado, 5 Con incidente

        public ObservableCollection<ShipmentModel> Items { get; } = new();
        public ObservableCollection<ShipmentModel> ViewItems { get; } = new();

        public ShipmentListViewModel(IShipmentRepository repo)
        {
            _repo = repo;

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Search) || e.PropertyName == nameof(StatusFilter))
                    ApplyFilter();
            };
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Items.Clear();
                ViewItems.Clear();

                var all = await _repo.GetAllAsync();
                foreach (var s in all.Values.OrderByDescending(x => x.CreatedDate))
                {
                    Items.Add(s);
                    ViewItems.Add(s);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            ViewItems.Clear();

            var q = (Search ?? string.Empty).Trim().ToLowerInvariant();
            var filtered = Items.AsEnumerable();

            if (StatusFilter != 0)
                filtered = filtered.Where(s => s.Status == StatusFilter);

            if (!string.IsNullOrEmpty(q))
                filtered = filtered.Where(s =>
                    (s.Code ?? "").ToLowerInvariant().Contains(q) ||
                    (s.ReceiverName ?? "").ToLowerInvariant().Contains(q) ||
                    (s.Description ?? "").ToLowerInvariant().Contains(q));

            foreach (var s in filtered)
                ViewItems.Add(s);
        }

        [RelayCommand]
        private async Task OpenAsync(ShipmentModel? s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Code)) return;
            await Shell.Current.GoToAsync($"{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(s.Code)}");
        }


        [RelayCommand]
        private async Task NewAsync()
        {
            await Shell.Current.GoToAsync(nameof(ShipmentFormPage));
        }
    }
}
