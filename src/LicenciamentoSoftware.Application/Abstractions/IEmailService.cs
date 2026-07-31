namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta de saída para envio de e-mails.
/// Implementada na camada de Infrastructure via SMTP (MailKit).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia um e-mail com corpo HTML.
    /// </summary>
    /// <param name="destinatario">Endereço de e-mail do destinatário.</param>
    /// <param name="assunto">Assunto do e-mail.</param>
    /// <param name="corpoHtml">Corpo do e-mail em HTML (já renderizado pelo TemplateRenderer).</param>
    Task EnviarAsync(
        string destinatario,
        string assunto,
        string corpoHtml,
        CancellationToken cancellationToken = default);
}
