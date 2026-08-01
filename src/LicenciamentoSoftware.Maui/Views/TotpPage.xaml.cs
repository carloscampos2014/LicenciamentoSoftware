using LicenciamentoSoftware.Maui.ViewModels;

namespace LicenciamentoSoftware.Maui.Views;

public partial class TotpPage : ContentPage
{
    public TotpPage(TotpViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
