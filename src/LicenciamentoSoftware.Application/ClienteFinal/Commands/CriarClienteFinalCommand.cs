namespace LicenciamentoSoftware.Application.ClienteFinal.Commands;

public sealed record CriarClienteFinalCommand(
    Guid IdCliente,
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone);
