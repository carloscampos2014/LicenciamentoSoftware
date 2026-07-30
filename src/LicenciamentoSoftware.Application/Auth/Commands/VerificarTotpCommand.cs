namespace LicenciamentoSoftware.Application.Auth.Commands;

/// <summary>
/// Segunda etapa do login quando 2FA está habilitado.
/// O token temporário foi emitido pelo LoginHandler.
/// </summary>
public sealed record VerificarTotpCommand(string TokenTemporario, string Codigo);
