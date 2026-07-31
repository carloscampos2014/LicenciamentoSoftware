using LicenciamentoSoftware.Client.Services;

namespace LicenciamentoSoftware.Web.Server.Configuration;

/// <summary>
/// Extensões de configuração do BFF (Backend for Frontend).
/// Centraliza o registro de cookie policy e HttpClients para a API.
/// </summary>
public static class BffServiceExtensions
{
    /// <summary>
    /// Configura a política de cookies HttpOnly; Secure; SameSite=Strict
    /// usada para armazenar o refresh token com segurança no browser.
    /// </summary>
    public static IServiceCollection AddBffCookiePolicy(
        this IServiceCollection services)
    {
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
            options.Secure = CookieSecurePolicy.Always;
            options.MinimumSameSitePolicy = SameSiteMode.Strict;
        });

        return services;
    }

    /// <summary>
    /// Configura os HttpClients dos services do Client com a BaseAddress da API.
    /// Chamado após AddApiClientServices() para aplicar a URL base.
    /// </summary>
    public static IServiceCollection ConfigureApiHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "ApiSettings:BaseUrl não configurado. Defina em appsettings.json ou variável de ambiente.");

        // Reconfigura cada HttpClient nomeado com a BaseAddress da API
        var clientTypes = new[]
        {
            typeof(AuthApiService),
            typeof(ClienteFinalApiService),
            typeof(UsuarioApiService),
            typeof(AplicacaoApiService),
            typeof(TipoLicencaApiService),
            typeof(LicencaApiService),
        };

        foreach (var clientType in clientTypes)
        {
            services.AddHttpClient(clientType.Name, client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
            });
        }

        return services;
    }
}
