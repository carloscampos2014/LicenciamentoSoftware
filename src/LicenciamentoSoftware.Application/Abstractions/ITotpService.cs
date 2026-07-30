namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta para geração e validação de TOTP (Time-based One-Time Password).
/// </summary>
public interface ITotpService
{
    /// <summary>Gera um novo segredo TOTP em Base32.</summary>
    string GerarSegredo();

    /// <summary>
    /// Retorna a URI otpauth:// para exibição como QR code no Google Authenticator / Authy.
    /// </summary>
    string GerarQrCodeUri(string segredo, string email, string emissor = "LicenciamentoSoftware");

    /// <summary>Valida o código TOTP de 6 dígitos contra o segredo.</summary>
    bool Validar(string segredo, string codigo);
}
