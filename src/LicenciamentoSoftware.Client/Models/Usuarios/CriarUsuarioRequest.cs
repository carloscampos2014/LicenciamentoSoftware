namespace LicenciamentoSoftware.Client.Models.Usuarios;

public sealed record CriarUsuarioRequest(
    string Nome,
    string Email,
    string Senha,
    string? Papel);
