using LicenciamentoSoftware.Maui.Services;

namespace LicenciamentoSoftware.Maui;

public partial class App : Application
{
    public App(AppShell shell, MauiAuthService authService)
    {
        InitializeComponent();
        MainPage = shell;

        // Tenta restaurar sessão do SecureStorage ao iniciar
        _ = Task.Run(async () =>
        {
            var restaurou = await authService.TentarRestaurarSessaoAsync();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (restaurou)
                    await Shell.Current.GoToAsync("//dashboard");
                else
                    await Shell.Current.GoToAsync("//login");
            });
        });
    }
}
