namespace LicenciamentoSoftware.Application.ClienteFinal.Commands;

public sealed record AtualizarClienteFinalCommand(
    Guid Id,
    string RazaoSocial,
    string Email,
    string? Telefone);
