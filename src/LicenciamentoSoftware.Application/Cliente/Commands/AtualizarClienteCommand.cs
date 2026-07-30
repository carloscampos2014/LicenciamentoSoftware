namespace LicenciamentoSoftware.Application.Cliente.Commands;

public sealed record AtualizarClienteCommand(
    Guid Id,
    string RazaoSocial,
    string Email,
    string? Telefone);
