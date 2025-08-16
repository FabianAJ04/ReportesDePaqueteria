using ReportesDePaqueteria.MVVVM.ViewModels;

namespace ReportesDePaqueteria.MVVVM.Views;

public partial class SignInView : ContentPage
{
	public SignInView(SignInViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}