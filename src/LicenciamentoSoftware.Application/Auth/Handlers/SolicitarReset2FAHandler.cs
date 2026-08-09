using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record SolicitarReset2FAResult
{
    /// <summary>Token enviado por e-mail — sempre retornar este (não vazar se e-mail existe).</summary>
    public sealed record Enviado : SolicitarReset2FAResult;
    /// <summary>Token temporário de login inválido ou expirado.</summary>
    public sealed record TokenLoginInvalido : SolicitarReset2FAResult;
    /// <summary>Senha incorreta.</summary>
    public sealed record SenhaIncorreta : SolicitarReset2FAResult;
}

public sealed record SolicitarReset2FACommand(
    /// <summary>Token temporário retornado pelo login (JWT de desafio 2FA).</summary>
    string TokenTemporario,
    string Senha,
    string? IpOrigem);

/// <summary>
/// Passo 1 do reset de 2FA: valida a senha e envia token de confirmação por e-mail.
/// O usuário é identificado pelo token temporário do desafio de 2FA (JWT de curta duração).
/// </summary>
public sealed class SolicitarReset2FAHandler
{
    private readonly IJwtTokenService _jwtService;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordHasher _hasher;
    private readonly ISolicitacaoReset2FARepository _solicitacaoRepo;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IClock _clock;
    private readonly string _portalUrl;

    public SolicitarReset2FAHandler(
        IJwtTokenService jwtService,
        IUsuarioRepository usuarioRepo,
        IPasswordHasher hasher,
        ISolicitacaoReset2FARepository solicitacaoRepo,
        IEmailService emailService,
        IEmailTemplateRenderer renderer,
        IClock clock,
        string portalUrl)
    {
        _jwtService       = jwtService;
        _usuarioRepo      = usuarioRepo;
        _hasher           = hasher;
        _solicitacaoRepo  = solicitacaoRepo;
        _emailService     = emailService;
        _renderer         = renderer;
        _clock            = clock;
        _portalUrl        = portalUrl.TrimEnd('/');
    }

    public async Task<SolicitarReset2FAResult> HandleAsync(
        SolicitarReset2FACommand command, CancellationToken ct = default)
    {
        // Resolver usuário pelo token temporário de login (mesmo padrão do VerificarTotpHandler)
        if (!_jwtService.ValidarAccessToken(command.TokenTemporario, out var idUsuario))
            return new SolicitarReset2FAResult.TokenLoginInvalido();

        var usuario = await _usuarioRepo.BuscarPorIdAsync(idUsuario, ct);
        if (usuario is null || !usuario.Ativo)
            return new SolicitarReset2FAResult.TokenLoginInvalido();

        // Validar senha — confirma que é o dono legítimo da conta
        if (usuario.SenhaHash is null || !_hasher.Verificar(command.Senha, usuario.SenhaHash))
            return new SolicitarReset2FAResult.SenhaIncorreta();

        // Gerar token de confirmação (32 bytes = 64 chars hex), expira em 15 min
        var tokenBruto = System.Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var tokenHash = System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(tokenBruto))).ToLowerInvariant();

        var expiraEm = _clock.UtcNow.AddMinutes(15);
        await _solicitacaoRepo.SalvarTokenAsync(
            usuario.Id, tokenHash, expiraEm, command.IpOrigem, ct);

        // Enviar e-mail com link de confirmação
        var link = $"{_portalUrl}/reset-2fa-confirmar?token={tokenBruto}";
        var variaveis = new Dictionary<string, string>
        {
            ["NomeUsuario"] = usuario.Nome,
            ["Link"]        = link,
            ["Token"]       = tokenBruto,
            ["Expiracao"]   = "15 minutos",
            ["AnoAtual"]    = _clock.UtcNow.Year.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
        };

        var corpo = _renderer.Renderizar("SolicitacaoReset2FA", variaveis);
        await _emailService.EnviarAsync(
            usuario.Email,
            "Confirmacao de reset do 2FA — LicenseManager",
            corpo, ct);

        return new SolicitarReset2FAResult.Enviado();
    }
}
