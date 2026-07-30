namespace LicenciamentoSoftware.Application.Usuario.Results;

public sealed record UsuarioResult(
    Guid Id,
    Guid IdCliente,
    string Nome,
    string Email,
    string Papel,
    bool Ativo);
