namespace LicenciamentoSoftware.Client.Models.ClientesFinais;

public sealed record CriarClienteFinalRequest(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone);
