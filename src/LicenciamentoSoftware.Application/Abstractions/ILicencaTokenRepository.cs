namespace LicenciamentoSoftware.Application.Abstractions;

public record LicencaTokenInfo(
    Guid Id,
    Guid IdLicenca,
    string SegredoHash,
    int ExpiracaoMinutos,
    DateTime CriadoEm,
    bool Ativo);

/// <summary>
/// Porta de persistência para tokens HMAC de licença.
/// </summary>
public interface ILicencaTokenRepository
{
    Task SalvarAsync(Guid id, Guid idLicenca, string segredoHash, int expiracaoMinutos,
        DateTime criadoEm, CancellationToken cancellationToken = default);

    Task<LicencaTokenInfo?> BuscarAtivoporLicencaAsync(Guid idLicenca,
        CancellationToken cancellationToken = default);

    Task RevogarPorLicencaAsync(Guid idLicenca,
        CancellationToken cancellationToken = default);

    Task AtualizarAsync(Guid id, string novoSegredoHash, int expiracaoMinutos,
        DateTime criadoEm, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------------------
    // Fase 8 — jobs de rotação e notificação
    // -------------------------------------------------------------------------

    /// <summary>
    /// Busca tokens ativos cuja expiração está próxima, para notificação ou rotação automática.
    /// Um token expira em <c>CriadoEm + ExpiracaoMinutos</c>.
    /// </summary>
    /// <param name="diasAntecedencia">Quantos dias antes do vencimento considerar "próximo".</param>
    Task<IReadOnlyList<Jobs.LicencaTokenJobInfo>> BuscarTokensProximosVencimentoAsync(
        int diasAntecedencia, CancellationToken cancellationToken = default);
}
