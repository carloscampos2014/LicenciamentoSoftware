using LicenciamentoSoftware.Application.Abstractions;
using System.Reflection;

namespace LicenciamentoSoftware.Infrastructure.Email;

/// <summary>
/// Renderiza templates de e-mail HTML embarcados no assembly como EmbeddedResource.
/// Substitui placeholders no formato <c>{{Chave}}</c> pelos valores fornecidos.
/// </summary>
public sealed class TemplateRenderer : IEmailTemplateRenderer
{
    // Namespace base dos recursos embarcados — deve coincidir com a estrutura de pastas
    private const string NamespaceBase =
        "LicenciamentoSoftware.Infrastructure.Email.Templates";

    private readonly Assembly _assembly;

    public TemplateRenderer()
        => _assembly = typeof(TemplateRenderer).Assembly;

    /// <inheritdoc/>
    public string Renderizar(string nomeTemplate, Dictionary<string, string> placeholders)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeTemplate);

        var resourceName = $"{NamespaceBase}.{nomeTemplate}.html";
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Template de e-mail não encontrado: '{resourceName}'. " +
                $"Verifique se o arquivo está marcado como EmbeddedResource.");

        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();

        // Injeta o ano atual automaticamente
        placeholders["{{AnoAtual}}"] = DateTime.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);

        foreach (var (chave, valor) in placeholders)
            html = html.Replace(chave, valor, StringComparison.Ordinal);

        return html;
    }
}
