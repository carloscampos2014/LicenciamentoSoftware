namespace LicenciamentoSoftware.Application.Cliente.Results;

/// <summary>DTO de saída para operações de Cliente.</summary>
public sealed record ClienteResult(
    Guid Id,
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone,
    bool Ativo);
