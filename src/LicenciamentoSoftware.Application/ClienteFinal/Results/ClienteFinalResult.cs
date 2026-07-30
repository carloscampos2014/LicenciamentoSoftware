namespace LicenciamentoSoftware.Application.ClienteFinal.Results;

public sealed record ClienteFinalResult(
    Guid Id,
    Guid IdCliente,
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone,
    bool Ativo);
