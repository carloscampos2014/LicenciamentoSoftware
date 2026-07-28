using LicenciamentoSoftware.Api.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LicenciamentoSoftware.Api.Configuration;

/// <summary>
/// Extensões de registro de serviços da API.
/// Mantém o Program.cs limpo — cada grupo de serviços tem seu próprio método.
/// </summary>
internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();

        // ProblemDetails + handler centralizado de exceções não tratadas.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiHealthChecks();

        return services;
    }

    private static IServiceCollection AddApiHealthChecks(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API operacional"));

        // Health check do banco de dados será adicionado na Fase 2,
        // quando a connection string estiver configurada:
        // .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)

        return services;
    }
}
