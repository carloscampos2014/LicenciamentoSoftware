using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

public interface ILicencaSessaoRepository
{
    // -------------------------------------------------------------------------
    // Leitura (Fase 6 — gestão manual)
    // -------------------------------------------------------------------------
    Task<SessaoResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SessaoResult>> ListarPorLicencaAsync(Guid idLicenca, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Escrita (Fase 6 — encerramento manual)
    // -------------------------------------------------------------------------
    Task EncerrarAsync(Guid id, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Fase 7 — validação de login
    // -------------------------------------------------------------------------

    /// <summary>
    /// Conta sessões ativas para a licença (todos os usuários).
    /// Usado para verificar o limite global antes de abrir nova sessão.
    /// Deve ser chamado dentro de transação serializável.
    /// </summary>
    Task<int> ContarAtivasPorLicencaAsync(Guid idLicenca, CancellationToken ct = default);

    /// <summary>
    /// Conta sessões ativas para um usuário específico dentro de uma licença.
    /// Usado para verificar o limite por usuário antes de abrir nova sessão.
    /// Deve ser chamado dentro de transação serializável.
    /// </summary>
    Task<int> ContarAtivasPorUsuarioAsync(
        Guid idLicenca, string identificadorUsuario, CancellationToken ct = default);

    /// <summary>
    /// Insere uma nova sessão ativa. Deve ser chamado dentro de transação serializável.
    /// </summary>
    Task InserirAsync(Domain.Entities.LicencaSessao sessao, CancellationToken ct = default);

    /// <summary>
    /// Atualiza <c>data_ultima_atividade</c> da sessão para o instante atual (heartbeat).
    /// </summary>
    Task AtualizarAtividadeAsync(Guid id, CancellationToken ct = default);
}
