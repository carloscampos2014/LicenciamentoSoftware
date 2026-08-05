using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Cliente.Abstractions;

public interface IClienteRepository
{
    Task<ClienteResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteInscricaoAsync(int tipoInscricao, string numeroInscricao, Guid? ignorarId = null, CancellationToken ct = default);
    Task<PagedResult<ClienteResult>> ListarAsync(string? razaoSocial, bool? ativo, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<Guid> InserirAsync(Domain.Entities.Cliente cliente, CancellationToken ct = default);
    Task AtualizarAsync(AtualizarClienteCommand command, CancellationToken ct = default);
    Task DesativarAsync(Guid id, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Fase 12.1 — Encerramento de conta de empresa
    // -------------------------------------------------------------------------

    /// <summary>
    /// Marca o cliente como encerrado: ativo = false, encerrado_em e exclusao_programada_em.
    /// </summary>
    Task EncerrarContaAsync(
        Guid id,
        DateTime encerradoEm,
        DateTime exclusaoProgramadaEm,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna IDs de clientes cuja <c>exclusao_programada_em</c> é menor ou igual a <paramref name="agora"/>.
    /// Usada pelo job de exclusão física diário.
    /// </summary>
    Task<IReadOnlyList<Guid>> BuscarClientesAgendadosParaExclusaoAsync(
        DateTime agora,
        CancellationToken ct = default);

    /// <summary>
    /// Exclui fisicamente o cliente e todos os registros vinculados (cascade).
    /// Deve ser chamado apenas pelo job após <c>exclusao_programada_em</c> ter passado.
    /// </summary>
    Task ExcluirFisicamenteAsync(Guid id, CancellationToken ct = default);
}
