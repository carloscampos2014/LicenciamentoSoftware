using LicenciamentoSoftware.Application.ClienteFinal.Commands;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.ClienteFinal.Abstractions;

public interface IClienteFinalRepository
{
    Task<ClienteFinalResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteInscricaoAsync(Guid idCliente, int tipoInscricao, string numeroInscricao, Guid? ignorarId = null, CancellationToken ct = default);
    Task<PagedResult<ClienteFinalResult>> ListarAsync(Guid? idCliente, string? razaoSocial, bool? ativo, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<Guid> InserirAsync(Domain.Entities.ClienteFinal clienteFinal, CancellationToken ct = default);
    Task AtualizarAsync(AtualizarClienteFinalCommand command, CancellationToken ct = default);
    Task DesativarAsync(Guid id, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Fase 12.1 — Notificação de encerramento de conta
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retorna e-mails e nomes dos clientes finais ativos vinculados ao tenant.
    /// Usado para envio de notificação de encerramento de conta.
    /// </summary>
    Task<IReadOnlyList<ClienteFinalEmailInfo>> ListarEmailsAtivosPorClienteAsync(
        Guid idCliente,
        CancellationToken ct = default);
}

/// <summary>Projeção mínima para notificação por e-mail ao cliente final.</summary>
public sealed record ClienteFinalEmailInfo(string RazaoSocial, string Email);
