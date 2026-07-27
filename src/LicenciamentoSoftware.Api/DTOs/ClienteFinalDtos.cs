namespace LicenciamentoSoftware.Api.DTOs;

public record ClienteFinalCreateRequest(Guid IdCliente, string RazaoSocial, int TipoInscricao, string NumeroInscricao, string Email, string? Telefone);
public record ClienteFinalUpdateRequest(string RazaoSocial, int TipoInscricao, string NumeroInscricao, string Email, string? Telefone, bool Ativo);
public record ClienteFinalResponse(Guid Id, Guid IdCliente, string RazaoSocial, int TipoInscricao, string NumeroInscricao, string Email, string? Telefone, bool Ativo);
