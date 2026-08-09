using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record AprovarReset2FAResult
{
    public sealed record Sucesso : AprovarReset2FAResult;
    public sealed record SolicitacaoNaoEncontrada : AprovarReset2FAResult;
    public sealed record JaProcessada : AprovarReset2FAResult;
}

/// <summary>
/// Passo 3 do reset de 2FA (Admin): aprova a solicitação Pendente,
/// executa o reset do TOTP e envia e-mail de aviso ao usuário.
/// </summary>
public sealed class AprovarReset2FAHandler
{
    private readonly ISolicitacaoReset2FARepository _solicitacaoRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public AprovarReset2FAHandler(
        ISolicitacaoReset2FARepository solicitacaoRepo,
        IUsuarioRepository usuarioRepo,
        IEmailService emailService,
        IEmailTemplateRenderer renderer,
        IUnitOfWork uow,
        IClock clock)
    {
        _solicitacaoRepo = solicitacaoRepo;
        _usuarioRepo     = usuarioRepo;
        _emailService    = emailService;
        _renderer        = renderer;
        _uow             = uow;
        _clock           = clock;
    }

    public async Task<AprovarReset2FAResult> HandleAsync(
        Guid idSolicitacao, CancellationToken ct = default)
    {
        var solicitacao = await _solicitacaoRepo.BuscarPorIdAsync(idSolicitacao, ct);

        if (solicitacao is null)
            return new AprovarReset2FAResult.SolicitacaoNaoEncontrada();

        if (solicitacao.Status != "Pendente")
            return new AprovarReset2FAResult.JaProcessada();

        var usuario = await _usuarioRepo.BuscarPorIdAsync(solicitacao.IdUsuario, ct);
        if (usuario is null)
            return new AprovarReset2FAResult.SolicitacaoNaoEncontrada();

        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            // Remove o TOTP (secret e pendente)
            await _usuarioRepo.AtualizarTotpSecretAsync(usuario.Id, null, ct);
            await _solicitacaoRepo.AprovarAsync(idSolicitacao, ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        // Enviar e-mail de aviso (fire-and-forget — não bloqueia o resultado)
        _ = EnviarEmailAvisoAsync(usuario.Email, usuario.Nome, ct);

        return new AprovarReset2FAResult.Sucesso();
    }

    private async Task EnviarEmailAvisoAsync(string email, string nome, CancellationToken ct)
    {
        try
        {
            var variaveis = new Dictionary<string, string>
            {
                ["NomeUsuario"] = nome,
                ["AnoAtual"]    = _clock.UtcNow.Year.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            };
            var corpo = _renderer.Renderizar("Reset2FAExecutado", variaveis);
            await _emailService.EnviarAsync(
                email,
                "Seu 2FA foi removido — LicenseManager",
                corpo, ct);
        }
        catch { /* log seria capturado pelo Serilog */ }
    }
}
