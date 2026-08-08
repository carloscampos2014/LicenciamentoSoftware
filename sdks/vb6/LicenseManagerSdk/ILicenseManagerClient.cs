using System.Runtime.InteropServices;

namespace LicenseManagerSdk;

/// <summary>
/// Interface COM para o cliente LicenseManager.
/// Exposta como ILicenseManagerClient para VB6 e outras linguagens COM.
/// </summary>
[ComVisible(true)]
[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface ILicenseManagerClient
{
    /// <summary>Valida login de um usuário. Retorna JSON com "authorized" e "sessionId".</summary>
    [DispId(1)]
    string Login(string userId);

    /// <summary>Envia heartbeat para manter a sessão ativa.</summary>
    [DispId(2)]
    void Heartbeat(string sessionId);

    /// <summary>Encerra a sessão (idempotente).</summary>
    [DispId(3)]
    void Logout(string sessionId);

    /// <summary>Valida ou registra instalação. Retorna JSON com "authorized", "installationId" e "alreadyRegistered".</summary>
    [DispId(4)]
    string ValidateInstallation(string machineId);
}
