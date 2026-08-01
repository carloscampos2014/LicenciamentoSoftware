using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

public interface ILicencaInstalacaoRepository
{
    // -------------------------------------------------------------------------
    // Leitura (Fase 6 — gestão manual)
    // -------------------------------------------------------------------------
    Task<InstalacaoRegistradaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<InstalacaoRegistradaResult>> ListarPorLicencaAsync(Guid idLicenca, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Escrita (Fase 6 — liberação manual)
    // -------------------------------------------------------------------------
    Task LiberarAsync(Guid id, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Fase 7 — validação de instalação
    // -------------------------------------------------------------------------

    /// <summary>
    /// Busca uma instalação registrada e ativa para a combinação licença + máquina.
    /// Retorna <c>null</c> se não encontrada.
    /// Deve ser chamado dentro de transação serializável.
    /// </summary>
    Task<InstalacaoRegistradaResult?> BuscarRegistradaAtivaAsync(
        Guid idLicenca, string identificadorMaquina, CancellationToken ct = default);

    /// <summary>
    /// Conta instalações ativas para a licença.
    /// Usado para verificar o limite antes de registrar nova instalação.
    /// Deve ser chamado dentro de transação serializável.
    /// </summary>
    Task<int> ContarAtivasAsync(Guid idLicenca, CancellationToken ct = default);

    /// <summary>
    /// Insere um novo registro de instalação.
    /// Deve ser chamado dentro de transação serializável.
    /// </summary>
    Task InserirRegistradaAsync(
        Domain.Entities.LicencaInstalacaoRegistrada instalacao, CancellationToken ct = default);

    /// <summary>
    /// Atualiza data_ultima_validacao de uma instalação registrada.
    /// Chamado após cada validação bem-sucedida (login de instalação ou heartbeat).
    /// </summary>
    Task AtualizarUltimaValidacaoAsync(Guid id, CancellationToken ct = default);
}
