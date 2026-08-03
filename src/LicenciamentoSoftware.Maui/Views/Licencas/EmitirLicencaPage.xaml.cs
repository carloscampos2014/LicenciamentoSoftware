using LicenciamentoSoftware.Maui.ViewModels.Licencas;

namespace LicenciamentoSoftware.Maui.Views.Licencas;

public partial class EmitirLicencaPage : ContentPage
{
    private readonly EmitirLicencaViewModel _vm;

    public EmitirLicencaPage(EmitirLicencaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearing();
    }
}
