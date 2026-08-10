using LicenciamentoSoftware.Maui.ViewModels.MinhaConta;

namespace LicenciamentoSoftware.Maui.Views.MinhaConta;

public partial class MinhaContaPage : ContentPage
{
    public MinhaContaPage(MinhaContaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MinhaContaViewModel vm)
            await vm.OnAppearing();
    }
}
