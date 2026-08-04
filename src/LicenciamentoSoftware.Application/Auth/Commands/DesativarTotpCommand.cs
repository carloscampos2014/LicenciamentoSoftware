namespace LicenciamentoSoftware.Application.Auth.Commands;

/// <summary>
/// Desativa o 2FA TOTP do usuário após confirmação com o código atual.
/// </summary>
public sealed record DesativarTotpCommand(Guid IdUsuario, string CodigoAtual);
