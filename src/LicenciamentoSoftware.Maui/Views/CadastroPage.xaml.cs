using LicenciamentoSoftware.Maui.ViewModels;

namespace LicenciamentoSoftware.Maui.Views;

public partial class CadastroPage : ContentPage
{
    public CadastroPage(CadastroViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
