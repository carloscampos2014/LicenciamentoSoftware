namespace LicenciamentoSoftware.Client.Models.Usuarios;

public sealed record UsuarioResult(
    Guid Id,
    Guid IdCliente,
    string Nome,
    string Email,
    string Papel,
    bool Ativo);
