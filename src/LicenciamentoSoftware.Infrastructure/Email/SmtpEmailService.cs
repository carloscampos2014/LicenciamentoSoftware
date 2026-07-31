using LicenciamentoSoftware.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LicenciamentoSoftware.Infrastructure.Email;

/// <summary>
/// Implementação de <see cref="IEmailService"/> via SMTP usando MailKit.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    private static readonly Action<ILogger, string, Exception?> _logDesabilitado =
        LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(1, "Smtp_Desabilitado"),
            "[SmtpEmailService] Envio desabilitado. Ignorando mensagem para {Destinatario}.");

    private static readonly Action<ILogger, string, string, Exception?> _logEnviado =
        LoggerMessage.Define<string, string>(LogLevel.Information,
            new EventId(2, "Smtp_Enviado"),
            "[SmtpEmailService] E-mail enviado para {Destinatario} — assunto: {Assunto}");

    private static readonly Action<ILogger, string, Exception?> _logErro =
        LoggerMessage.Define<string>(LogLevel.Error,
            new EventId(3, "Smtp_Erro"),
            "[SmtpEmailService] Falha ao enviar e-mail para {Destinatario}.");

    public SmtpEmailService(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }

    public async Task EnviarAsync(
        string destinatario,
        string assunto,
        string corpoHtml,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Habilitado)
        {
            _logDesabilitado(_logger, destinatario, null);
            return;
        }

        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_settings.NomeRemetente, _settings.EmailRemetente));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = assunto;
        mensagem.Body    = new BodyBuilder { HtmlBody = corpoHtml }.ToMessageBody();

        using var cliente = new SmtpClient();

        try
        {
            var socketOptions = _settings.UsarSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await cliente.ConnectAsync(_settings.Host, _settings.Porta, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Usuario))
                await cliente.AuthenticateAsync(_settings.Usuario, _settings.Senha, cancellationToken);

            await cliente.SendAsync(mensagem, cancellationToken);
            await cliente.DisconnectAsync(quit: true, cancellationToken);

            _logEnviado(_logger, destinatario, assunto, null);
        }
        catch (Exception ex)
        {
            _logErro(_logger, destinatario, ex);
            throw;
        }
    }
}
