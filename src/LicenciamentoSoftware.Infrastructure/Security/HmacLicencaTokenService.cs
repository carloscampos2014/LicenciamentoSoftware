using LicenciamentoSoftware.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace LicenciamentoSoftware.Infrastructure.Security;

/// <summary>
/// Implementação do serviço HMAC-SHA256 para tokens de licença.
/// Usa apenas primitivas da BCL — sem dependências externas adicionais.
/// </summary>
public sealed class HmacLicencaTokenService : IHmacLicencaTokenService
{
    // 32 bytes = 256 bits — resistente a força bruta
    private const int TamanhoSegredoBytes = 32;

    /// <inheritdoc/>
    public string GerarSegredo()
    {
        var bytes = new byte[TamanhoSegredoBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc/>
    public string GerarAssinatura(
        Guid idLicenca,
        string payload,
        string timestampUtc,
        string segredoTexto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segredoTexto);
        ArgumentException.ThrowIfNullOrWhiteSpace(timestampUtc);

        var mensagem = MontarMensagem(idLicenca, payload, timestampUtc);
        var chave = Encoding.UTF8.GetBytes(segredoTexto);

        var hash = HMACSHA256.HashData(chave, Encoding.UTF8.GetBytes(mensagem));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public bool ValidarAssinatura(
        Guid idLicenca,
        string payload,
        string timestampUtc,
        string segredoTexto,
        string assinaturaRecebida)
    {
        if (string.IsNullOrWhiteSpace(segredoTexto)
            || string.IsNullOrWhiteSpace(timestampUtc)
            || string.IsNullOrWhiteSpace(assinaturaRecebida))
            return false;

        var assinaturaEsperada = GerarAssinatura(idLicenca, payload, timestampUtc, segredoTexto);

        // Comparação em tempo constante para evitar timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(assinaturaEsperada),
            Encoding.UTF8.GetBytes(assinaturaRecebida.ToLowerInvariant()));
    }

    /// <inheritdoc/>
    public string HashSegredo(string segredoTexto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segredoTexto);
        // BCrypt com work factor 12 — adequado para segredos de longa duração
        return BCrypt.Net.BCrypt.HashPassword(segredoTexto, workFactor: 12);
    }

    /// <inheritdoc/>
    public bool VerificarHashSegredo(string segredoTexto, string hash)
    {
        if (string.IsNullOrWhiteSpace(segredoTexto) || string.IsNullOrWhiteSpace(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(segredoTexto, hash);
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    /// <summary>
    /// Monta a string canônica que será assinada:
    /// <c>{idLicenca}:{timestampUtc}:{payload}</c>
    /// </summary>
    private static string MontarMensagem(Guid idLicenca, string payload, string timestampUtc)
        => $"{idLicenca:D}:{timestampUtc}:{payload ?? string.Empty}";
}
