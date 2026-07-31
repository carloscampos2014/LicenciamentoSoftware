namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Interface base para todos os jobs agendados.
/// Implementações são registradas no DI e executadas pelo <c>JobScheduler</c>.
/// A interface é propositalmente simples para facilitar migração futura para Hangfire/Quartz.
/// </summary>
public interface IScheduledJob
{
    /// <summary>Nome legível do job — usado em logs e monitoramento.</summary>
    string Nome { get; }

    /// <summary>Executa o job.</summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
