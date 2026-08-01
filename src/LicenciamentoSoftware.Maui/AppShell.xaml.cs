using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.Views;
using LicenciamentoSoftware.Maui.Views.Aplicacoes;
using LicenciamentoSoftware.Maui.Views.ClientesFinais;
using LicenciamentoSoftware.Maui.Views.Licencas;
using LicenciamentoSoftware.Maui.Views.Usuarios;

namespace LicenciamentoSoftware.Maui;

public partial class AppShell : Shell
{
    private readonly MauiAuthService _authService;

    public AppShell(MauiAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        // Registra rotas não listadas no flyout
        Routing.RegisterRoute("totp",    typeof(TotpPage));
        Routing.RegisterRoute("cadastro", typeof(CadastroPage));
        Routing.RegisterRoute("licencas/emitir", typeof(EmitirLicencaPage));

        Navigating += OnNavigating;
    }

    /// <summary>
    /// Guard de navegação — redireciona para login se não autenticado
    /// ao tentar acessar rotas protegidas.
    /// </summary>
    private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        var rota = e.Target.Location.OriginalString;
        var rotasPublicas = new[] { "//login", "totp", "cadastro" };

        if (rotasPublicas.Any(r => rota.Contains(r)))
            return;

        if (!_authService.Autenticado)
        {
            e.Cancel();
            await GoToAsync("//login");
        }
    }
}
