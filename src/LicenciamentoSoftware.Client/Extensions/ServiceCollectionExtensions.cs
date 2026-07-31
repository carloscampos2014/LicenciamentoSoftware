using LicenciamentoSoftware.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LicenciamentoSoftware.Client.Extensions;

/// <summary>
/// Registra os serviços HTTP do Client.
/// Chamado tanto pelo BFF Server quanto pelo MAUI.
/// O HttpClient com BaseAddress e handler de autenticação é configurado
/// pelo projeto consumidor (Server ou MAUI).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiClientServices(this IServiceCollection services)
    {
        services.AddHttpClient<AuthApiService>();
        services.AddHttpClient<ClienteFinalApiService>();
        services.AddHttpClient<UsuarioApiService>();
        services.AddHttpClient<AplicacaoApiService>();
        services.AddHttpClient<TipoLicencaApiService>();
        services.AddHttpClient<LicencaApiService>();

        return services;
    }
}
