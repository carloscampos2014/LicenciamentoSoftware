using LicenciamentoSoftware.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LicenciamentoSoftware.Infrastructure.Jobs;

/// <summary>
/// BackgroundService que orquestra todos os jobs agendados.
/// Cada job é resolvido do DI a cada execução (escopo por execução).
/// </summary>
public sealed class JobScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobScheduler> _logger;
    private readonly JobSettings _settings;

    private static readonly Action<ILogger, int, Exception?> _logIniciado =
        LoggerMessage.Define<int>(LogLevel.Information,
            new EventId(1, "JobScheduler_Iniciado"),
            "[JobScheduler] Iniciado. Aguardando primeiro ciclo em {Delay}s.");

    private static readonly Action<ILogger, string, Exception?> _logResolveFalhou =
        LoggerMessage.Define<string>(LogLevel.Error,
            new EventId(2, "JobScheduler_ResolveFalhou"),
            "[JobScheduler] Falha ao resolver o job {Job}.");

    private static readonly Action<ILogger, string, Exception?> _logExecutando =
        LoggerMessage.Define<string>(LogLevel.Information,
            new EventId(3, "JobScheduler_Executando"),
            "[JobScheduler] Executando job {Job}.");

    private static readonly Action<ILogger, string, Exception?> _logConcluido =
        LoggerMessage.Define<string>(LogLevel.Information,
            new EventId(4, "JobScheduler_Concluido"),
            "[JobScheduler] Job {Job} concluído.");

    private static readonly Action<ILogger, string, Exception?> _logCancelado =
        LoggerMessage.Define<string>(LogLevel.Warning,
            new EventId(5, "JobScheduler_Cancelado"),
            "[JobScheduler] Job {Job} cancelado.");

    private static readonly Action<ILogger, string, Exception?> _logFalhou =
        LoggerMessage.Define<string>(LogLevel.Error,
            new EventId(6, "JobScheduler_Falhou"),
            "[JobScheduler] Job {Job} falhou.");

    public JobScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<JobScheduler> logger,
        IOptions<JobSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _settings     = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logIniciado(_logger, _settings.DelayInicialSegundos, null);
        await Task.Delay(TimeSpan.FromSeconds(_settings.DelayInicialSegundos), stoppingToken);

        var tarefas = new[]
        {
            ExecutarComTimerAsync<EncerrarSessoesInativasJob>(
                _settings.SessoesInativasIntervaloMinutos, stoppingToken),
            ExecutarComTimerAsync<ExpirarLicencasPeriodoJob>(
                _settings.ExpiracaoLicencasIntervaloMinutos, stoppingToken),
            ExecutarComTimerAsync<RenovarLicencasAutomaticasJob>(
                _settings.RenovacaoAutomaticaIntervaloMinutos, stoppingToken),
            ExecutarComTimerAsync<RotacionarTokensLicencaJob>(
                _settings.RotacaoTokensIntervaloMinutos, stoppingToken),
            ExecutarComTimerAsync<NotificarExpiracaoJob>(
                _settings.NotificacaoIntervaloMinutos, stoppingToken),
            ExecutarComTimerAsync<ExcluirEmpresasEncerradasJob>(
                _settings.ExclusaoEmpresasIntervaloMinutos, stoppingToken),
        };

        await Task.WhenAll(tarefas);
    }

    private async Task ExecutarComTimerAsync<TJob>(
        int intervaloMinutos, CancellationToken stoppingToken)
        where TJob : IScheduledJob
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervaloMinutos));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ExecutarJobAsync<TJob>(stoppingToken);
    }

    private async Task ExecutarJobAsync<TJob>(CancellationToken stoppingToken)
        where TJob : IScheduledJob
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        IScheduledJob job;
        try
        {
            job = scope.ServiceProvider.GetRequiredService<TJob>();
        }
        catch (Exception ex)
        {
            _logResolveFalhou(_logger, typeof(TJob).Name, ex);
            return;
        }

        _logExecutando(_logger, job.Nome, null);

        try
        {
            await job.ExecuteAsync(stoppingToken);
            _logConcluido(_logger, job.Nome, null);
        }
        catch (OperationCanceledException)
        {
            _logCancelado(_logger, job.Nome, null);
        }
        catch (Exception ex)
        {
            _logFalhou(_logger, job.Nome, ex);
        }
    }
}
