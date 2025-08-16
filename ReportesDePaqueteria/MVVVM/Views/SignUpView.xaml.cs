using ReportesDePaqueteria.MVVVM.ViewModels;

namespace ReportesDePaqueteria.MVVVM.Views;

public partial class SignUpView : ContentPage
{
	public SignUpView(SignUpViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;	
    }
}