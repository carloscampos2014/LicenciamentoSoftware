namespace LicenciamentoSoftware.Infrastructure.Email;

/// <summary>
/// Configurações SMTP para envio de e-mail — lidas de <c>EmailSettings</c> no appsettings.
/// Credenciais sensíveis devem ser fornecidas via secrets ou variáveis de ambiente.
/// </summary>
public sealed class EmailSettings
{
    /// <summary>Habilita ou desabilita o envio de e-mails (útil para desabilitar em desenvolvimento).</summary>
    public bool Habilitado { get; set; }

    /// <summary>Endereço do servidor SMTP (ex: smtp.gmail.com, smtp.sendgrid.net).</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Porta SMTP (587 para STARTTLS, 465 para SSL, 25 para relay interno).</summary>
    public int Porta { get; set; } = 587;

    /// <summary>Usar SSL na conexão SMTP.</summary>
    public bool UsarSsl { get; set; }

    /// <summary>Usuário para autenticação SMTP (geralmente o e-mail do remetente).</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Senha SMTP — fornecer via secrets.json ou variável de ambiente.</summary>
    public string Senha { get; set; } = string.Empty;

    /// <summary>Endereço de e-mail do remetente.</summary>
    public string EmailRemetente { get; set; } = string.Empty;

    /// <summary>Nome de exibição do remetente (ex: "LicenciamentoSoftware").</summary>
    public string NomeRemetente { get; set; } = "LicenciamentoSoftware";
}
