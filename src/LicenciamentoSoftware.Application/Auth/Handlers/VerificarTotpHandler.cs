using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public sealed class VerificarTotpHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITotpService _totpService;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClock _clock;

    public VerificarTotpHandler(
        IUsuarioRepository usuarioRepository,
        ITotpService totpService,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock)
    {
        _usuarioRepository = usuarioRepository;
        _totpService = totpService;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
        _clock = clock;
    }

    public async Task<AuthResult> HandleAsync(
        VerificarTotpCommand command,
        CancellationToken cancellationToken = default)
    {
        // Valida o token temporário de desafio e extrai o ID do usuário
        if (!_jwtService.ValidarAccessToken(command.TokenTemporario, out var idUsuario))
            return new AuthResult.TotpInvalido("Token de desafio inválido ou expirado.");

        var usuario = await _usuarioRepository
            .BuscarPorIdAsync(idUsuario, cancellationToken);

        if (usuario is null || !usuario.Ativo)
            return new AuthResult.TotpInvalido("Usuário não encontrado ou inativo.");

        if (usuario.TotpSecretHash is null)
            return new AuthResult.TotpInvalido("2FA não está habilitado para este usuário.");

        if (!_totpService.Validar(usuario.TotpSecretHash, command.Codigo))
            return new AuthResult.TotpInvalido("Código TOTP inválido.");

        // TOTP válido — emite JWT completo
        var papel = await _usuarioRepository
            .BuscarPapelAsync(usuario.Id, cancellationToken);

        var tokenPar = _jwtService.GerarTokenPar(
            usuario.Id, usuario.IdCliente, usuario.Nome, papel, usuario.Email);

        await _refreshTokenRepository.SalvarAsync(
            usuario.Id,
            tokenPar.RefreshToken,
            _clock.UtcNow.AddDays(30),
            cancellationToken);

        return new AuthResult.Sucesso(
            tokenPar.AccessToken,
            tokenPar.RefreshToken,
            tokenPar.AccessTokenExpiracao,
            usuario.Nome,
            papel);
    }
}
