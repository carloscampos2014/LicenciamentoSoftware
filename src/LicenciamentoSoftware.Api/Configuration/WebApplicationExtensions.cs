using Hangfire;
using LicenciamentoSoftware.Api.Middleware;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Infrastructure.Jobs;
using LicenciamentoSoftware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

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
            // OpenAPI JSON: GET /openapi/v1.json
            app.MapOpenApi();

            // Scalar UI: GET /scalar/v1
            app.MapScalarApiReference(options =>
            {
                options.Title = "LicenciamentoSoftware API";
                options.Theme = ScalarTheme.Purple;
            });
        }

        // HTTPS redirect — desabilitado em desenvolvimento para permitir HTTP do celular Android
        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        // CORS — antes de Authentication para que preflight OPTIONS seja respondido corretamente
        app.UseCors("BffPolicy");

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

        // Dashboard Hangfire — protegido por Basic Auth
        var hangfireUser     = app.Configuration["HangfireSettings:Usuario"] ?? "admin";
        var hangfirePassword = app.Configuration["HangfireSettings:Senha"]   ?? "changeme";

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireBasicAuthFilter(hangfireUser, hangfirePassword)],
            DashboardTitle = "LicenseManager — Jobs",
            DisplayStorageConnectionString = false,
        });

        // Registrar jobs recorrentes via cron
        var cfg = app.Configuration;

        RecurringJob.AddOrUpdate(
            "encerrar-sessoes-inativas",
            (EncerrarSessoesInativasJob job) => job.ExecuteAsync(CancellationToken.None),
            cfg["HangfireSettings:CronSessoesInativas"] ?? "*/5 * * * *");

        RecurringJob.AddOrUpdate(
            "expirar-licencas-periodo",
            (ExpirarLicencasPeriodoJob job) => job.ExecuteAsync(CancellationToken.None),
            cfg["HangfireSettings:CronExpiracaoLicencas"] ?? "0 * * * *");

        RecurringJob.AddOrUpdate(
            "renovar-licencas-automaticas",
            (RenovarLicencasAutomaticasJob job) => job.ExecuteAsync(CancellationToken.None),
            cfg["HangfireSettings:CronRenovacaoAutomatica"] ?? "15 * * * *");

        RecurringJob.AddOrUpdate(
            "rotacionar-tokens-licenca",
            (RotacionarTokensLicencaJob job) => job.ExecuteAsync(CancellationToken.None),
            cfg["HangfireSettings:CronRotacaoTokens"] ?? "0 2 * * *");

        RecurringJob.AddOrUpdate(
            "notificar-expiracao",
            (NotificarExpiracaoJob job) => job.ExecuteAsync(CancellationToken.None),
            cfg["HangfireSettings:CronNotificacao"] ?? "0 8 * * *");

        RecurringJob.AddOrUpdate(
            "excluir-empresas-encerradas",
            (ExcluirEmpresasEncerradasJob job) => job.ExecuteAsync(CancellationToken.None),
            cfg["HangfireSettings:CronExclusaoEmpresas"] ?? "0 3 * * *");

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
