using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels;
using LicenciamentoSoftware.Maui.ViewModels.Aplicacoes;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using LicenciamentoSoftware.Maui.ViewModels.ClientesFinais;
using LicenciamentoSoftware.Maui.ViewModels.Licencas;
using LicenciamentoSoftware.Maui.ViewModels.Usuarios;
using LicenciamentoSoftware.Maui.Views;
using LicenciamentoSoftware.Maui.Views.Aplicacoes;
using LicenciamentoSoftware.Maui.Views.ClientesFinais;
using LicenciamentoSoftware.Maui.Views.Licencas;
using LicenciamentoSoftware.Maui.Views.Usuarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace LicenciamentoSoftware.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",   "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf",  "OpenSansSemibold");
            });

        // ── Configuração via appsettings.json embarcado ───────────────────────
        var asm    = Assembly.GetExecutingAssembly();
        var stream = asm.GetManifestResourceStream(
            "LicenciamentoSoftware.Maui.Resources.Raw.appsettings.json");

        if (stream is not null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
            ?? "https://localhost:7075";

        // ── Services (Singleton — mesma instância por toda a vida do app) ─────
        builder.Services.AddSingleton(new MauiApiClientFactory(apiBaseUrl));
        builder.Services.AddSingleton<MauiAuthService>();

        // ── Shell ─────────────────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();

        // ── ViewModels — Transient (nova instância por navegação) ─────────────
        // Auth
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<TotpViewModel>();
        builder.Services.AddTransient<CadastroViewModel>();

        // Gestão
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ListaClientesFinaisViewModel>();
        builder.Services.AddTransient<ListaUsuariosViewModel>();
        builder.Services.AddTransient<ListaAplicacoesViewModel>();
        builder.Services.AddTransient<ListaLicencasViewModel>();
        builder.Services.AddTransient<EmitirLicencaViewModel>();

        // ── Views — Transient ─────────────────────────────────────────────────
        // Auth
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<TotpPage>();
        builder.Services.AddTransient<CadastroPage>();

        // Gestão
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ListaClientesFinaisPage>();
        builder.Services.AddTransient<ListaUsuariosPage>();
        builder.Services.AddTransient<ListaAplicacoesPage>();
        builder.Services.AddTransient<ListaLicencasPage>();
        builder.Services.AddTransient<EmitirLicencaPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
