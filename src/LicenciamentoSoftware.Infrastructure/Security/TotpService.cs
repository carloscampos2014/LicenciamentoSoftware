using LicenciamentoSoftware.Application.Abstractions;
using OtpNet;
using System.Text;

namespace LicenciamentoSoftware.Infrastructure.Security;

/// <summary>
/// Implementação de TOTP usando OtpNet.
/// Compatível com Google Authenticator, Authy e similares.
/// </summary>
public sealed class TotpService : ITotpService
{
    // Janela de ±1 intervalo (30s) para tolerância de drift de relógio
    private const int JanelaDeToleranncia = 1;

    public string GerarSegredo()
    {
        var chave = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(chave);
    }

    public string GerarQrCodeUri(string segredo, string email, string emissor = "LicenciamentoSoftware")
    {
        var emailEncoded = Uri.EscapeDataString(email);
        var emissorEncoded = Uri.EscapeDataString(emissor);
        return $"otpauth://totp/{emissorEncoded}:{emailEncoded}?secret={segredo}&issuer={emissorEncoded}&algorithm=SHA1&digits=6&period=30";
    }

    public bool Validar(string segredo, string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            return false;

        var chave = Base32Encoding.ToBytes(segredo);
        var totp = new Totp(chave);
        return totp.VerifyTotp(
            codigo,
            out _,
            new VerificationWindow(JanelaDeToleranncia, JanelaDeToleranncia));
    }
}
