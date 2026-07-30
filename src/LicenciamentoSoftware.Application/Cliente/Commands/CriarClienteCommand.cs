namespace LicenciamentoSoftware.Application.Cliente.Commands;

public sealed record CriarClienteCommand(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone);
