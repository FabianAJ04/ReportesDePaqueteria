using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace ReportesDePaqueteria.MVVM.ViewModels
{

    public class ShipmentViewModel
    {
        private readonly ShipmentRepository _repository; // Repositorio para manejar los envíos
        public ShipmentModel Shipment { get; set; } = new ShipmentModel(); // Objeto para crear o editar un envío
        public ObservableCollection<ShipmentModel> lstShipments { get; set; } = 
        new ObservableCollection<ShipmentModel>(); // Colección observable para la interfaz de usuario

        // Comandos
        public ICommand CreateCommand { get; set; }
        public ICommand LoadCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand EditCommand { get; set; }

        public INavigation _navigation { get; set; } // Navegación para cambiar de vistas

        public ShipmentsViewModel(INavigation navigation)
        {
            _repository = new ShipmentsRepository();
            CreateCommand = new Command(async () => await CrearShipment());
            LoadCommand = new Command(async () => await LoadShipment());
            DeleteCommand = new Command<String>(async (Code) => await DeleteShipment(Code));
            EditCommand = new Command(async () => await UpdateShipment(Shipment));
            _navigation = navigation;
        }


        private async Task UpdateShipment(ShipmentModel newShipment)
        {
            try
            {
                if (Validar())
                {
                    await _repository.UpdateDocumentAsync(newShipment, newShipment.Code);
                    await ShowMessage("Envio modificado exitosamente!", true);
                    Console.WriteLine("Envio modificado exitosamente!");
                    _navigation.PopAsync();
                    Clean();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrio un error al modificar el envio. Error: {ex.ToString()}");
            }

        }

        private async Task DeleteShipment(string Code)
        {
            try
            {
                bool isComfirmed = await App.Current.MainPage.DisplayAlert(
                    "Alerta",
                    "Desar borrar el envio?",
                    "Si",
                    "No");

                if (!isComfirmed) return;

                await _repository.DeleteDocumentAsync(Code);
                await LoadShipment();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrio un error al borrar el envio. Error: {ex.ToString()}");
            }
        }

        private async Task LoadShipment()
        {
            try
            {
                var ShipmentsFromDB = await _repository.GetAllAsync();

                lstShipments.Clear();

                foreach (var Shipment in ShipmentsFromDB)
                {
                    lstShipments.Add(new ShipmentModel
                    {
                        Code = Shipment.Code,
                        Nombre = Shipment.Value.Nombre,
                        Telefono = Shipment.Value.Telefono,
                        Correo = Shipment.Value.Correo,
                        Direccion = Shipment.Value.Direccion
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrio un error al cargar los envios. Error: {ex.ToString()}");
            }
        }

        private async Task CrearShipment()
        {
            try
            {
                if (Validar())
                {
                    await _repository.CreateDocumentAsync(Shipment);
                    await ShowMessage("Envio agregado exitosamente!", true);
                    Console.WriteLine("Envio agregado exitosamente!");
                    _navigation.PopAsync();
                    Clean();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrio un error al crear el envio. Error: {ex.ToString()}");
            }

        }

        private bool Validar()
        {
            bool respuesta = true;
            if (string.IsNullOrWhiteSpace(Shipment.Nombre) ||
               string.IsNullOrWhiteSpace(Shipment.Telefono) ||
               string.IsNullOrWhiteSpace(Shipment.Correo) ||
               string.IsNullOrWhiteSpace(Shipment.Direccion))
            {
                ShowMessage("Por favor completar todos los campos", false);
                respuesta = false;
            }
            return respuesta;
        }

        private void Clean()
        {
            Shipment = new ShipmentModel();
        }

        private async Task ShowMessage(string message, bool isSuccess)
        {
            await App.Current.MainPage.DisplayAlert(
                    isSuccess ? "Informacíon" : "Error",
                    message,
                    "Ok");
        }
    }
}
