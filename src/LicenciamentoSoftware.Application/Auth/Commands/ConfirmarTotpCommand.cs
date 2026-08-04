namespace LicenciamentoSoftware.Application.Auth.Commands;

/// <summary>
/// Confirma que o QR code foi escaneado corretamente validando o primeiro código TOTP.
/// Chamado após o setup inicial para garantir que o autenticador está funcionando.
/// </summary>
public sealed record ConfirmarTotpCommand(Guid IdUsuario, string Codigo);
