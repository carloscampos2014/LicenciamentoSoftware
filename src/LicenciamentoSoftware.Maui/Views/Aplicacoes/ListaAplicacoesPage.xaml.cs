using LicenciamentoSoftware.Maui.ViewModels.Aplicacoes;

namespace LicenciamentoSoftware.Maui.Views.Aplicacoes;

public partial class ListaAplicacoesPage : ContentPage
{
    private readonly ListaAplicacoesViewModel _vm;

    public ListaAplicacoesPage(ListaAplicacoesViewModel vm)
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
