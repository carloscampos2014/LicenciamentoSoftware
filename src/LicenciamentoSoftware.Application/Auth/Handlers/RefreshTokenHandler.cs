using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IJwtTokenService _jwtService;
    private readonly IClock _clock;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUsuarioRepository usuarioRepository,
        IJwtTokenService jwtService,
        IClock clock)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _usuarioRepository = usuarioRepository;
        _jwtService = jwtService;
        _clock = clock;
    }

    public async Task<AuthResult> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var tokenInfo = await _refreshTokenRepository
            .BuscarPorHashAsync(command.RefreshToken, cancellationToken);

        if (tokenInfo is null)
            return new AuthResult.TokenInvalido("Refresh token não encontrado.");

        if (tokenInfo.Revogado)
            return new AuthResult.TokenInvalido("Refresh token revogado.");

        if (tokenInfo.Expiracao < _clock.UtcNow)
            return new AuthResult.TokenInvalido("Refresh token expirado.");

        var usuario = await _usuarioRepository
            .BuscarPorIdAsync(tokenInfo.IdUsuario, cancellationToken);

        if (usuario is null || !usuario.Ativo)
            return new AuthResult.TokenInvalido("Usuário inativo ou não encontrado.");

        // Rotaciona — revoga o token atual e emite novo par
        await _refreshTokenRepository.RevogarAsync(tokenInfo.Id, cancellationToken);

        var papel = await _usuarioRepository
            .BuscarPapelAsync(usuario.Id, cancellationToken);

        var novoTokenPar = _jwtService.GerarTokenPar(
            usuario.Id, usuario.IdCliente, usuario.Nome, papel);

        await _refreshTokenRepository.SalvarAsync(
            usuario.Id,
            novoTokenPar.RefreshToken,
            _clock.UtcNow.AddDays(30),
            cancellationToken);

        return new AuthResult.Sucesso(
            novoTokenPar.AccessToken,
            novoTokenPar.RefreshToken,
            novoTokenPar.AccessTokenExpiracao,
            usuario.Nome,
            papel);
    }
}
