namespace LicenciamentoSoftware.Application.Abstractions;

public record RefreshTokenInfo(
    Guid Id,
    Guid IdUsuario,
    string TokenHash,
    DateTime Expiracao,
    bool Revogado);

public interface IRefreshTokenRepository
{
    Task SalvarAsync(Guid idUsuario, string tokenHash, DateTime expiracao,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenInfo?> BuscarPorHashAsync(string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevogarAsync(Guid id, CancellationToken cancellationToken = default);
    Task RevogarTodosDoUsuarioAsync(Guid idUsuario, CancellationToken cancellationToken = default);
}
