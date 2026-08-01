using LicenciamentoSoftware.Maui.ViewModels.Licencas;

namespace LicenciamentoSoftware.Maui.Views.Licencas;

public partial class ListaLicencasPage : ContentPage
{
    private readonly ListaLicencasViewModel _vm;

    public ListaLicencasPage(ListaLicencasViewModel vm)
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
