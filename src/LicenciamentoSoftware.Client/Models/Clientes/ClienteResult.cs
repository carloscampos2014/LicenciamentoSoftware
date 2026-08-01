namespace LicenciamentoSoftware.Client.Models.Clientes;

public sealed record ClienteResult(
    Guid Id,
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone,
    bool Ativo);
