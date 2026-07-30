namespace LicenciamentoSoftware.Application.Usuario.Commands;

public sealed record AtualizarUsuarioCommand(
    Guid Id,
    string Nome,
    string Email);
