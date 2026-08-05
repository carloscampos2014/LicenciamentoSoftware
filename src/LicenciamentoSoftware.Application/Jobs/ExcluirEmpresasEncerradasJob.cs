using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Job diário que exclui fisicamente clientes cuja <c>exclusao_programada_em</c>
/// já passou. A exclusão em cascata (FK ON DELETE CASCADE) remove todos os dados
/// vinculados ao tenant: usuários, clientes finais, aplicações, licenças, logs.
///
/// Agendamento:
///   - Encerramento padrão: executa 90 dias após o encerramento.
///   - Encerramento imediato: executa na próxima rodada do job (≤ 24h).
/// </summary>
public sealed class ExcluirEmpresasEncerradasJob : IScheduledJob
{
    public string Nome => "ExcluirEmpresasEncerradas";

    private readonly IClienteRepository _clienteRepo;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExcluirEmpresasEncerradasJob> _logger;

    private static readonly Action<ILogger, Guid, Exception?> _logExcluindo =
        LoggerMessage.Define<Guid>(LogLevel.Information,
            new EventId(1, "ExcluirEmpresas_Excluindo"),
            "[ExcluirEmpresasEncerradas] Excluindo fisicamente cliente {IdCliente}.");

    private static readonly Action<ILogger, Guid, Exception?> _logErro =
        LoggerMessage.Define<Guid>(LogLevel.Error,
            new EventId(2, "ExcluirEmpresas_Erro"),
            "[ExcluirEmpresasEncerradas] Erro ao excluir cliente {IdCliente}.");

    private static readonly Action<ILogger, int, Exception?> _logConcluido =
        LoggerMessage.Define<int>(LogLevel.Information,
            new EventId(3, "ExcluirEmpresas_Concluido"),
            "[ExcluirEmpresasEncerradas] Concluído. {Total} cliente(s) excluído(s).");

    public ExcluirEmpresasEncerradasJob(
        IClienteRepository clienteRepo,
        IClock clock,
        IUnitOfWork uow,
        ILogger<ExcluirEmpresasEncerradasJob> logger)
    {
        _clienteRepo = clienteRepo;
        _clock       = clock;
        _uow         = uow;
        _logger      = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var agora = _clock.UtcNow;
        var ids = await _clienteRepo.BuscarClientesAgendadosParaExclusaoAsync(agora, cancellationToken);

        int excluidos = 0;

        foreach (var id in ids)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                _logExcluindo(_logger, id, null);
                await _uow.BeginAsync(cancellationToken: cancellationToken);
                await _clienteRepo.ExcluirFisicamenteAsync(id, cancellationToken);
                await _uow.CommitAsync(cancellationToken);
                excluidos++;
            }
            catch (Exception ex)
            {
                _logErro(_logger, id, ex);
                try { await _uow.RollbackAsync(cancellationToken); } catch { /* ignore */ }
            }
        }

        _logConcluido(_logger, excluidos, null);
    }
}
