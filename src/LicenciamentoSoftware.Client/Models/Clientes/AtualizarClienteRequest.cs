namespace LicenciamentoSoftware.Client.Models.Clientes;

public sealed record AtualizarClienteRequest(
    string RazaoSocial,
    string Email,
    string? Telefone);
