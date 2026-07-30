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
}
