using LicenciamentoSoftware.Maui.ViewModels.ClientesFinais;

namespace LicenciamentoSoftware.Maui.Views.ClientesFinais;

public partial class ListaClientesFinaisPage : ContentPage
{
    private readonly ListaClientesFinaisViewModel _vm;

    public ListaClientesFinaisPage(ListaClientesFinaisViewModel vm)
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
