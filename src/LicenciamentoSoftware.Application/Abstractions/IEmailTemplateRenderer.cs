namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta para renderização de templates de e-mail HTML.
/// Lê o template pelo nome, substitui os placeholders e retorna o HTML pronto.
/// Implementada na Infrastructure como leitura de <c>EmbeddedResource</c>.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renderiza um template HTML substituindo os placeholders informados.
    /// </summary>
    /// <param name="nomeTemplate">
    /// Nome do template sem extensão (ex: <c>"LicencaExpirando"</c>).
    /// Corresponde ao arquivo <c>Email/Templates/{nomeTemplate}.html</c>.
    /// </param>
    /// <param name="placeholders">
    /// Dicionário de substituições no formato <c>{{Chave}}</c> → valor.
    /// </param>
    string Renderizar(string nomeTemplate, Dictionary<string, string> placeholders);
}
