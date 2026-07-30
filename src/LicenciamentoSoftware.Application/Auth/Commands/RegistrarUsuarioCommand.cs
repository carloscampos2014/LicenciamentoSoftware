namespace LicenciamentoSoftware.Application.Auth.Commands;

/// <summary>
/// Registra um novo usuário vinculado a um cliente.
/// O primeiro usuário registrado para um cliente recebe papel AdministradorCliente.
/// </summary>
public sealed record RegistrarUsuarioCommand(
    Guid IdCliente,
    string Nome,
    string Email,
    string Senha);
