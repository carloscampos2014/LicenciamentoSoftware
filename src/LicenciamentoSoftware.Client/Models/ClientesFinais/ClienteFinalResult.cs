namespace LicenciamentoSoftware.Client.Models.ClientesFinais;

public sealed record ClienteFinalResult(
    Guid Id,
    Guid IdCliente,
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone,
    bool Ativo);
