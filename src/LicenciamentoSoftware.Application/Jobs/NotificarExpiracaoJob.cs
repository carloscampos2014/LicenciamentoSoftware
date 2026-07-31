using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Envia e-mails de notificação para administradores de clientes cujas
/// licenças ou tokens estão próximos do vencimento.
/// O envio é fire-and-forget por licença: falha em um e-mail não impede os demais.
/// </summary>
public sealed class NotificarExpiracaoJob : IScheduledJob
{
    public string Nome => "NotificarExpiracao";

    private readonly ILicencaGestaoRepository _licencaRepo;
    private readonly ILicencaTokenRepository _tokenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmailService _email;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IClock _clock;
    private readonly ILogger<NotificarExpiracaoJob> _logger;
    private readonly int _diasAntecedencia;

    private static readonly Action<ILogger, string, Guid, Guid, Exception?> _logSemAdmin =
        LoggerMessage.Define<string, Guid, Guid>(LogLevel.Warning,
            new EventId(1, "Notificar_SemAdmin"),
            "[{Job}] Nenhum admin encontrado para cliente {IdCliente}. Licença/Token {IdRegistro} ignorado.");

    private static readonly Action<ILogger, string, string, Guid, Exception?> _logEnviado =
        LoggerMessage.Define<string, string, Guid>(LogLevel.Information,
            new EventId(2, "Notificar_Enviado"),
            "[{Job}] Notificação enviada para {Email} — registro {Id}.");

    private static readonly Action<ILogger, string, Guid, Exception?> _logErro =
        LoggerMessage.Define<string, Guid>(LogLevel.Error,
            new EventId(3, "Notificar_Erro"),
            "[{Job}] Erro ao notificar registro {Id}.");

    public NotificarExpiracaoJob(
        ILicencaGestaoRepository licencaRepo,
        ILicencaTokenRepository tokenRepo,
        IUsuarioRepository usuarioRepo,
        IEmailService email,
        IEmailTemplateRenderer templateRenderer,
        IClock clock,
        ILogger<NotificarExpiracaoJob> logger,
        int diasAntecedencia = 7)
    {
        _licencaRepo      = licencaRepo;
        _tokenRepo        = tokenRepo;
        _usuarioRepo      = usuarioRepo;
        _email            = email;
        _templateRenderer = templateRenderer;
        _clock            = clock;
        _logger           = logger;
        _diasAntecedencia = diasAntecedencia;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var agora = _clock.UtcNow;
        await NotificarLicencasAsync(agora, cancellationToken);
        await NotificarTokensAsync(cancellationToken);
    }

    private async Task NotificarLicencasAsync(DateTime agora, CancellationToken ct)
    {
        var licencas = await _licencaRepo.BuscarLicencasProximasVencimentoAsync(
            agora, _diasAntecedencia, ct);

        foreach (var licenca in licencas)
        {
            try
            {
                var admin = await _usuarioRepo.BuscarEmailAdminPorClienteAsync(licenca.IdCliente, ct);
                if (admin is null)
                {
                    _logSemAdmin(_logger, Nome, licenca.IdCliente, licenca.IdLicenca, null);
                    continue;
                }

                var diasRestantes = (int)(_clock.UtcNow - licenca.DataFim).TotalDays;
                diasRestantes = Math.Abs(diasRestantes);

                var corpo = _templateRenderer.Renderizar("LicencaExpirando", new Dictionary<string, string>
                {
                    ["{{NomeAdmin}}"]       = admin.Nome,
                    ["{{NomeAplicacao}}"]   = licenca.NomeAplicacao,
                    ["{{DataVencimento}}"]  = licenca.DataFim.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    ["{{DiasRestantes}}"]   = diasRestantes.ToString(CultureInfo.InvariantCulture),
                });

                await _email.EnviarAsync(
                    admin.Email,
                    $"Licença \"{licenca.NomeAplicacao}\" vence em {diasRestantes} dia(s)",
                    corpo, ct);

                _logEnviado(_logger, Nome, admin.Email, licenca.IdLicenca, null);
            }
            catch (Exception ex)
            {
                _logErro(_logger, Nome, licenca.IdLicenca, ex);
            }
        }
    }

    private async Task NotificarTokensAsync(CancellationToken ct)
    {
        var tokens = await _tokenRepo.BuscarTokensProximosVencimentoAsync(_diasAntecedencia, ct);

        foreach (var token in tokens)
        {
            try
            {
                var admin = await _usuarioRepo.BuscarEmailAdminPorClienteAsync(token.IdCliente, ct);
                if (admin is null)
                {
                    _logSemAdmin(_logger, Nome, token.IdCliente, token.IdToken, null);
                    continue;
                }

                var venceEm       = token.CriadoEm.AddMinutes(token.ExpiracaoMinutos);
                var diasRestantes = (int)(venceEm - _clock.UtcNow).TotalDays;

                var corpo = _templateRenderer.Renderizar("TokenExpirando", new Dictionary<string, string>
                {
                    ["{{NomeAdmin}}"]       = admin.Nome,
                    ["{{NomeAplicacao}}"]   = token.NomeAplicacao,
                    ["{{DataVencimento}}"]  = venceEm.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    ["{{DiasRestantes}}"]   = diasRestantes.ToString(CultureInfo.InvariantCulture),
                });

                await _email.EnviarAsync(
                    admin.Email,
                    $"Token da licença \"{token.NomeAplicacao}\" vence em {diasRestantes} dia(s)",
                    corpo, ct);

                _logEnviado(_logger, Nome, admin.Email, token.IdToken, null);
            }
            catch (Exception ex)
            {
                _logErro(_logger, Nome, token.IdToken, ex);
            }
        }
    }
}
