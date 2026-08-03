using LicenciamentoSoftware.Maui.ViewModels;

namespace LicenciamentoSoftware.Maui.Views;

public partial class LoginPage : ContentPage
{
    private const double LarguraMinimaSplit = 600; // abaixo disso oculta o painel esquerdo

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        AjustarLayout(width);
    }

    private void AjustarLayout(double width)
    {
        var mostrarBranding = width >= LarguraMinimaSplit;

        PainelBranding.IsVisible = mostrarBranding;

        // Em mobile: formulário ocupa tela toda; em desktop: split 50/50
        RootGrid.ColumnDefinitions[0] = mostrarBranding
            ? new ColumnDefinition(new GridLength(3, GridUnitType.Star))
            : new ColumnDefinition(new GridLength(0));

        RootGrid.ColumnDefinitions[1] = new ColumnDefinition(new GridLength(7, GridUnitType.Star));
    }
}
