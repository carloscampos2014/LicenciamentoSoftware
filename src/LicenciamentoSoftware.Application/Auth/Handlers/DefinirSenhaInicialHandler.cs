using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

/// <summary>
/// Define nova senha para conta anonimizada após exclusão LGPD.
///
/// Fluxo:
/// 1. Valida o token temporário de papel "DefinirSenha" emitido pelo LoginHandler
/// 2. Confirma que o usuário realmente não tem senha (protege contra uso indevido do token)
/// 3. Define a nova senha e emite JWT completo (faz login automaticamente)
/// </summary>
public sealed class DefinirSenhaInicialHandler
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwtService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;

    public DefinirSenhaInicialHandler(
        IUsuarioRepository usuarioRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IPasswordHasher hasher,
        IJwtTokenService jwtService,
        IClock clock,
        IUnitOfWork uow)
    {
        _usuarioRepo      = usuarioRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _hasher           = hasher;
        _jwtService       = jwtService;
        _clock            = clock;
        _uow              = uow;
    }

    public async Task<AuthResult> HandleAsync(
        DefinirSenhaInicialCommand command,
        CancellationToken ct = default)
    {
        // 1. Validar e extrair claims do token temporário
        var claims = _jwtService.ValidarToken(command.TokenTemporario);
        if (claims is null)
            return new AuthResult.TokenInvalido("Token inválido ou expirado.");

        var papel = claims.FindFirst("role")?.Value
                 ?? claims.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (papel != "DefinirSenha")
            return new AuthResult.TokenInvalido("Token não autorizado para esta operação.");

        var sub = claims.FindFirst("sub")?.Value
               ?? claims.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(sub, out var idUsuario))
            return new AuthResult.TokenInvalido("Token inválido.");

        // 2. Buscar usuário e confirmar que ainda está sem senha
        var usuario = await _usuarioRepo.BuscarPorIdAsync(idUsuario, ct);
        if (usuario is null || !usuario.Ativo)
            return new AuthResult.TokenInvalido("Usuário não encontrado.");

        if (!string.IsNullOrEmpty(usuario.SenhaHash))
            return new AuthResult.TokenInvalido("Este usuário já possui senha. Use o login normal.");

        // 3. Validar a nova senha
        if (string.IsNullOrWhiteSpace(command.NovaSenha) || command.NovaSenha.Length < 8)
            return new AuthResult.Negado("A senha deve ter no mínimo 8 caracteres.");

        // 4. Definir a nova senha e emitir JWT completo
        var senhaHash = _hasher.Hash(command.NovaSenha);
        var papel2 = await _usuarioRepo.BuscarPapelAsync(idUsuario, ct);
        var tokenPar = _jwtService.GerarTokenPar(
            usuario.Id, usuario.IdCliente, usuario.Nome, papel2, usuario.Email);

        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            await _usuarioRepo.DefinirSenhaAsync(idUsuario, senhaHash, ct);

            await _refreshTokenRepo.SalvarAsync(
                usuario.Id,
                tokenPar.RefreshToken,
                _clock.UtcNow.AddDays(30),
                ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return new AuthResult.Sucesso(
            tokenPar.AccessToken,
            tokenPar.RefreshToken,
            tokenPar.AccessTokenExpiracao,
            usuario.Nome,
            papel2);
    }
}
