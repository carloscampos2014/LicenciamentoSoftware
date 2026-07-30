using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var tokenInfo = await _refreshTokenRepository
            .BuscarPorHashAsync(command.RefreshToken, cancellationToken);

        if (tokenInfo is null || tokenInfo.Revogado)
            return; // Idempotente — já revogado ou inexistente

        await _refreshTokenRepository.RevogarAsync(tokenInfo.Id, cancellationToken);
    }
}
