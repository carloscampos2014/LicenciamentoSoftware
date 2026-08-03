using LicenciamentoSoftware.Maui.Services;

namespace LicenciamentoSoftware.Maui;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly MauiAuthService _authService;

    public App(AppShell shell, MauiAuthService authService)
    {
        InitializeComponent();
        _shell = shell;
        _authService = authService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);

        // Restaura sessão após a janela estar pronta
        window.Created += OnWindowCreated;

        return window;
    }

    private async void OnWindowCreated(object? sender, EventArgs e)
    {
        // Aguarda o Shell estar completamente inicializado
        await Task.Delay(100);

        try
        {
            var restaurou = await _authService.TentarRestaurarSessaoAsync();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is null) return;

                if (restaurou)
                    // Navega diretamente sem mostrar login
                    await Shell.Current.GoToAsync("//dashboard");
                else
                    await Shell.Current.GoToAsync("//login");
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//login");
            });
        }
    }
}
