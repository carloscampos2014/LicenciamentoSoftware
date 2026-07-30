namespace LicenciamentoSoftware.Application.Usuario.Commands;

public sealed record CriarUsuarioCommand(
    Guid IdCliente,
    string Nome,
    string Email,
    string Senha,
    string Papel = "OperadorCliente");
