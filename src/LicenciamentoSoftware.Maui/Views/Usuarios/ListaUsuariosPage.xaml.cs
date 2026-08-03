using LicenciamentoSoftware.Maui.ViewModels.Usuarios;

namespace LicenciamentoSoftware.Maui.Views.Usuarios;

public partial class ListaUsuariosPage : ContentPage
{
    private readonly ListaUsuariosViewModel _vm;

    public ListaUsuariosPage(ListaUsuariosViewModel vm)
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
