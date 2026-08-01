using LicenciamentoSoftware.Maui.ViewModels;

namespace LicenciamentoSoftware.Maui.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
