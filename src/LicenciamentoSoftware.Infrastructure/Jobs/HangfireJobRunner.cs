using LicenciamentoSoftware.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Infrastructure.Jobs;

/// <summary>
/// Wrapper que resolve e executa um <see cref="IScheduledJob"/> dentro de um escopo DI
/// criado pelo Hangfire a cada execução.
/// </summary>
public static class HangfireJobRunner
{
    private static readonly Action<ILogger, string, Exception?> _logResolveFalhou =
        LoggerMessage.Define<string>(LogLevel.Error,
            new EventId(10, "HangfireJobRunner_ResolveFalhou"),
            "[Hangfire] Falha ao resolver o job {Job}.");

    private static readonly Action<ILogger, string, Exception?> _logIniciando =
        LoggerMessage.Define<string>(LogLevel.Information,
            new EventId(11, "HangfireJobRunner_Iniciando"),
            "[Hangfire] Iniciando job {Job}.");

    private static readonly Action<ILogger, string, Exception?> _logConcluido =
        LoggerMessage.Define<string>(LogLevel.Information,
            new EventId(12, "HangfireJobRunner_Concluido"),
            "[Hangfire] Job {Job} concluído.");

    private static readonly Action<ILogger, string, Exception?> _logFalhou =
        LoggerMessage.Define<string>(LogLevel.Error,
            new EventId(13, "HangfireJobRunner_Falhou"),
            "[Hangfire] Job {Job} falhou.");

    public static async Task RunAsync<TJob>(
        IServiceProvider sp,
        ILogger<TJob> logger,
        CancellationToken ct = default)
        where TJob : IScheduledJob
    {
        TJob job;
        try
        {
            job = sp.GetRequiredService<TJob>();
        }
        catch (Exception ex)
        {
            _logResolveFalhou(logger, typeof(TJob).Name, ex);
            throw;
        }

        _logIniciando(logger, job.Nome, null);
        try
        {
            await job.ExecuteAsync(ct);
            _logConcluido(logger, job.Nome, null);
        }
        catch (Exception ex)
        {
            _logFalhou(logger, job.Nome, ex);
            throw; // relança para que o Hangfire registre como falha e faça retry
        }
    }
}
