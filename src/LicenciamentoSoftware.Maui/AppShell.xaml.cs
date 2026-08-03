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
    private const double LarguraDesktop = 900;

    public AppShell(MauiAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        Routing.RegisterRoute("totp",            typeof(TotpPage));
        Routing.RegisterRoute("cadastro",        typeof(CadastroPage));
        Routing.RegisterRoute("licencas/emitir", typeof(EmitirLicencaPage));

        Navigating += OnNavigating;
        Navigated  += OnNavigated;

        // Valor inicial: começa em loading (splash) enquanto verifica sessão
        // A App.xaml.cs navega para //dashboard ou //login após verificar SecureStorage
        FlyoutBehavior = FlyoutBehavior.Disabled;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

#if ANDROID
        // Muda para Flyout DEPOIS que handler foi configurado (evita crash do renderer)
        FlyoutBehavior = FlyoutBehavior.Flyout;
#endif

        if (Window is not null)
            Window.SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        if (Window is null) return;
        AjustarFlyout(Window.Width);
    }

    // ── Layout adaptativo ─────────────────────────────────────────────────────

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0) AjustarFlyout(width);
    }

    private void AjustarFlyout(double width)
    {
#if ANDROID
        // Android: sempre Flyout (hambúrguer) — Locked em tela pequena é inutilizável
        FlyoutBehavior = FlyoutBehavior.Flyout;
#else
        // Windows/Desktop: Locked quando tela larga o suficiente
        FlyoutBehavior = width >= LarguraDesktop
            ? FlyoutBehavior.Locked
            : FlyoutBehavior.Flyout;
#endif
    }

    // ── Navegação ─────────────────────────────────────────────────────────────

    private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        var rota = e.Target.Location.OriginalString;
        var rotasPublicas = new[] { "//login", "//loading", "totp", "cadastro" };

        if (rotasPublicas.Any(r => rota.Contains(r))) return;

        if (!_authService.Autenticado)
        {
            e.Cancel();
            await GoToAsync("//login");
        }
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        // Reativa FlyoutBehavior correto após navegar da tela de loading
        var rota = e.Current.Location.OriginalString;
        if (rota.Contains("dashboard") || rota.Contains("clientes") ||
            rota.Contains("aplicacoes") || rota.Contains("licencas") ||
            rota.Contains("usuarios"))
        {
#if ANDROID
            FlyoutBehavior = FlyoutBehavior.Flyout;
#else
            AjustarFlyout(Window?.Width ?? 900);
#endif
        }
    }

    // ── Botão voltar Android ──────────────────────────────────────────────────

    protected override bool OnBackButtonPressed()
    {
        // Só fecha se o app já estiver inicializado com uma rota válida
        var rota = CurrentState?.Location?.OriginalString;
        if (string.IsNullOrEmpty(rota)) return false;

        Application.Current?.Quit();
        return true;
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        await GoToAsync("//login");
    }
}
