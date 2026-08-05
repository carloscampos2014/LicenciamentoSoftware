using LicenciamentoSoftware.Maui.ViewModels.MinhaEmpresa;

namespace LicenciamentoSoftware.Maui.Views.MinhaEmpresa;

public partial class MinhaEmpresaPage : ContentPage
{
    private readonly MinhaEmpresaViewModel _vm;

    public MinhaEmpresaPage(MinhaEmpresaViewModel vm)
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
