using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

/// <summary>
/// Inicia o fluxo de recuperação de senha por e-mail.
/// Gera um token seguro, armazena o hash e envia o link por e-mail.
/// Sempre retorna sucesso (mesmo se e-mail não existir) para não vazar informações.
/// </summary>
public sealed class EsqueciSenhaHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRecuperacaoSenhaRepository _recuperacaoRepo;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IClock _clock;
    private readonly string _portalUrl;

    public EsqueciSenhaHandler(
        IUsuarioRepository usuarioRepository,
        IRecuperacaoSenhaRepository recuperacaoRepo,
        IEmailService emailService,
        IEmailTemplateRenderer renderer,
        IClock clock,
        string portalUrl)
    {
        _usuarioRepository = usuarioRepository;
        _recuperacaoRepo   = recuperacaoRepo;
        _emailService      = emailService;
        _renderer          = renderer;
        _clock             = clock;
        _portalUrl         = portalUrl.TrimEnd('/');
    }

    public async Task HandleAsync(string email, CancellationToken ct = default)
    {
        // Sempre retorna sem erro — não vazar se e-mail existe
        var usuario = await _usuarioRepository.BuscarPorEmailAsync(email, ct);
        if (usuario is null || !usuario.Ativo) return;

        // Gerar token seguro (32 bytes = 64 chars hex) e calcular o hash SHA-256
        var tokenBruto = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var tokenHash  = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(tokenBruto))).ToLowerInvariant();

        var expiraEm = _clock.UtcNow.AddHours(1);
        await _recuperacaoRepo.SalvarAsync(usuario.Id, tokenHash, expiraEm, ct);

        var link = $"{_portalUrl}/redefinir-senha?token={tokenBruto}";
        var variaveis = new Dictionary<string, string>
        {
            ["NomeUsuario"] = usuario.Nome,
            ["Link"]        = link,
            ["Expiracao"]   = "1 hora",
            ["AnoAtual"]    = _clock.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        var corpo = _renderer.Renderizar("RecuperacaoSenha", variaveis);
        await _emailService.EnviarAsync(
            destinatario: email,
            assunto: "Redefinição de senha — LicenseManager",
            corpoHtml: corpo,
            cancellationToken: ct);
    }
}
