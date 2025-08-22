using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class ShipmentFormViewModel : ObservableObject
    {
        private readonly IShipmentRepository _shipments;
        private readonly IUserRepository _users;

        [ObservableProperty] private string origin = "";
        [ObservableProperty] private string destination = "";
        [ObservableProperty] private string? receiverName;
        [ObservableProperty] private string? description;
        [ObservableProperty] private bool isBusy;

        public ShipmentFormViewModel(IShipmentRepository shipments, IUserRepository users)
        {
            _shipments = shipments;
            _users = users;
        }

        [RelayCommand]
        private async Task CreateAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(ReceiverName))
            {
                await Shell.Current.DisplayAlert("Validación", "Ingresa el nombre del destinatario.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                // Sender = usuario logueado
                var uid = await SecureStorage.GetAsync("user_id");
                UserModel? sender = null;
                if (!string.IsNullOrWhiteSpace(uid))
                    sender = await _users.GetByIdAsync(uid);

                // Generar código único
                string code;
                int guard = 0;
                do
                {
                    code = TrackingGenerator.NewCode();
                    guard++;
                    if (guard > 12) throw new Exception("No se pudo generar un código único.");
                }
                while ((await _shipments.GetByCodeAsync(code)) != null);

                var shipment = new ShipmentModel
                {
                    Code = code,
                    Sender = sender,
                    Worker = null,
                    ReceiverName = ReceiverName?.Trim() ?? "",
                    Status = 1, // Enviado
                    CreatedDate = DateTime.UtcNow,
                    Description = Description?.Trim() ?? "",
                    Incident = null,
                    Origin = Origin?.Trim() ?? "",
                    Destination = Destination?.Trim() ?? ""
                };

                await _shipments.CreateAsync(shipment);

                await Shell.Current.DisplayAlert("Éxito", $"Envío creado: {shipment.Code}", "OK");
                await Shell.Current.GoToAsync($"{nameof(ShipmentDetailPage)}?code={Uri.EscapeDataString(shipment.Code)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo crear el envío: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
