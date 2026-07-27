namespace LicenciamentoSoftware.Api.DTOs;

public record ClienteCreateRequest(string RazaoSocial, int TipoInscricao, string NumeroInscricao, string Email, string? Telefone);
public record ClienteUpdateRequest(string RazaoSocial, int TipoInscricao, string NumeroInscricao, string Email, string? Telefone, bool Ativo);
public record ClienteResponse(Guid Id, string RazaoSocial, int TipoInscricao, string NumeroInscricao, string Email, string? Telefone, bool Ativo);
