using LicenciamentoSoftware.Api.Middleware;
using LicenciamentoSoftware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LicenciamentoSoftware.Api.Configuration;

/// <summary>
/// Extensões de configuração do pipeline HTTP.
/// Centraliza a ordem de middlewares fora do Program.cs.
/// </summary>
internal static class WebApplicationExtensions
{
    internal static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Tratamento global de erros — deve ser o primeiro middleware.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        // Rate limiting — antes de Authentication para rejeitar cedo
        app.UseRateLimiter();

        // Ordem obrigatória: Authentication antes de Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Anti-replay aplicado apenas nos endpoints de validação de licença
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/api/validacao"),
            branch => branch.UseMiddleware<AntiReplayMiddleware>());

        app.MapControllers();

        // Health check acessível em /health
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
        });

        return app;
    }

    /// <summary>
    /// Aplica migrations pendentes do DbUp na inicialização da aplicação.
    /// Idempotente — scripts já aplicados são ignorados.
    /// </summary>
    internal static WebApplication UseDatabaseMigrations(this WebApplication app)
    {
        var migrator = app.Services.GetRequiredService<DatabaseMigrator>();
        migrator.MigrateUp();
        return app;
    }
}
