namespace LicenciamentoSoftware.Application.Auth.Commands;

public sealed record ConfigurarTotpCommand(Guid IdUsuario, string Email);
