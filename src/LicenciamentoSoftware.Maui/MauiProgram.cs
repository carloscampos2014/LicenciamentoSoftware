using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels;
using LicenciamentoSoftware.Maui.ViewModels.Aplicacoes;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using LicenciamentoSoftware.Maui.ViewModels.ClientesFinais;
using LicenciamentoSoftware.Maui.ViewModels.Licencas;
using LicenciamentoSoftware.Maui.ViewModels.MinhaEmpresa;
using LicenciamentoSoftware.Maui.ViewModels.MinhaConta;
using LicenciamentoSoftware.Maui.Views.MinhaConta;
using LicenciamentoSoftware.Maui.ViewModels.Usuarios;
using LicenciamentoSoftware.Maui.Views;
using LicenciamentoSoftware.Maui.Views.Aplicacoes;
using LicenciamentoSoftware.Maui.Views.ClientesFinais;
using LicenciamentoSoftware.Maui.Views.Licencas;
using LicenciamentoSoftware.Maui.Views.MinhaEmpresa;
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

        // ── Configuração via appsettings.json — lido dos assets em runtime ──────
        // FileSystem.OpenAppPackageFileAsync lê assets/ em runtime (não usa assembly compilado)
        try
        {
            var stream = Task.Run(async () =>
                await FileSystem.OpenAppPackageFileAsync("appsettings.json")).GetAwaiter().GetResult();
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);
        }
        catch
        {
            // Fallback via ManifestResource (compatibilidade Windows)
            var asm = Assembly.GetExecutingAssembly();
            var fallbackStream = asm.GetManifestResourceStream(
                "LicenciamentoSoftware.Maui.Resources.Raw.appsettings.json");
            if (fallbackStream is not null)
            {
                var config = new ConfigurationBuilder().AddJsonStream(fallbackStream).Build();
                builder.Configuration.AddConfiguration(config);
            }
        }

#if ANDROID
        // Override de URL para dev Android via appsettings.android.json (não versionado)
        // Crie Resources/Raw/appsettings.android.json com: { "ApiSettings": { "BaseUrl": "http://IP:5016" } }
        // Este arquivo está no .gitignore — nunca é commitado
        try
        {
            var androidStream = Task.Run(async () =>
                await FileSystem.OpenAppPackageFileAsync("appsettings.android.json")).GetAwaiter().GetResult();
            var androidConfig = new ConfigurationBuilder().AddJsonStream(androidStream).Build();
            builder.Configuration.AddConfiguration(androidConfig);
        }
        catch { /* arquivo opcional — não existe em produção */ }
#endif

        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
            ?? "https://localhost:7075";

        // ── Services (Singleton — mesma instância por toda a vida do app) ─────
        builder.Services.AddSingleton(new MauiApiClientFactory(apiBaseUrl));
        builder.Services.AddSingleton<MauiAuthService>();

        // ── Shell ─────────────────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();

        // ── ViewModels — Singleton para telas do flyout (preserva estado e evita reload duplo)
        //                Transient para fluxos de navegação push (auth, emitir)
        // Auth — Transient (sempre fresh ao navegar)
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<TotpViewModel>();
        builder.Services.AddTransient<CadastroViewModel>();

        // Gestão — Singleton (flyout reutiliza a mesma instância; flag Carregado preservado)
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<ListaClientesFinaisViewModel>();
        builder.Services.AddSingleton<ListaUsuariosViewModel>();
        builder.Services.AddSingleton<ListaAplicacoesViewModel>();
        builder.Services.AddSingleton<ListaLicencasViewModel>();
        builder.Services.AddSingleton<MinhaEmpresaViewModel>();
        builder.Services.AddSingleton<MinhaContaViewModel>();

        // Emitir — Transient (wizard sempre começa do zero)
        builder.Services.AddTransient<EmitirLicencaViewModel>();

        // ── Views — Singleton para telas do flyout, Transient para fluxos push ──
        // Auth
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<TotpPage>();
        builder.Services.AddTransient<CadastroPage>();
        builder.Services.AddTransient<LoadingPage>();

        // Gestão (Singleton — mesma instância enquanto app está aberto)
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<ListaClientesFinaisPage>();
        builder.Services.AddSingleton<ListaUsuariosPage>();
        builder.Services.AddSingleton<ListaAplicacoesPage>();
        builder.Services.AddSingleton<ListaLicencasPage>();
        builder.Services.AddSingleton<MinhaEmpresaPage>();
        builder.Services.AddSingleton<MinhaContaPage>();

        // Emitir — Transient (sempre novo)
        builder.Services.AddTransient<EmitirLicencaPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
