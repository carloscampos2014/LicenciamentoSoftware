namespace LicenciamentoSoftware.Client.Models.ClientesFinais;

public sealed record AtualizarClienteFinalRequest(
    string RazaoSocial,
    string Email,
    string? Telefone);
