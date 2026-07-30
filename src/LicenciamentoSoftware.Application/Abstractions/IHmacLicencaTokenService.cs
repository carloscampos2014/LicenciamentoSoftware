namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta para operações criptográficas do token HMAC-SHA256 por licença.
/// </summary>
public interface IHmacLicencaTokenService
{
    /// <summary>
    /// Gera um segredo aleatório criptograficamente seguro.
    /// O valor retornado é exibido UMA ÚNICA VEZ ao emissor — armazene apenas o hash.
    /// </summary>
    string GerarSegredo();

    /// <summary>
    /// Gera a assinatura HMAC-SHA256 para um payload dado o segredo em texto.
    /// </summary>
    /// <param name="idLicenca">ID da licença dona do token.</param>
    /// <param name="payload">Corpo da requisição (string canônica ou vazio).</param>
    /// <param name="timestampUtc">Timestamp UTC da requisição (ISO-8601).</param>
    /// <param name="segredoTexto">Segredo em texto puro (nunca armazenado).</param>
    string GerarAssinatura(Guid idLicenca, string payload, string timestampUtc, string segredoTexto);

    /// <summary>
    /// Valida a assinatura HMAC-SHA256 contra o segredo em texto.
    /// </summary>
    bool ValidarAssinatura(Guid idLicenca, string payload, string timestampUtc,
        string segredoTexto, string assinaturaRecebida);

    /// <summary>
    /// Computa o hash BCrypt do segredo para armazenamento seguro no banco.
    /// </summary>
    string HashSegredo(string segredoTexto);

    /// <summary>
    /// Verifica se o segredo em texto corresponde ao hash armazenado.
    /// </summary>
    bool VerificarHashSegredo(string segredoTexto, string hash);
}
