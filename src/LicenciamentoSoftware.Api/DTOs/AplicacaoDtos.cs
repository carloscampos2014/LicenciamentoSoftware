namespace LicenciamentoSoftware.Api.DTOs;

public record AplicacaoCreateRequest(Guid IdCliente, string Titulo, string? Descricao, Guid IdTipoLicenca);
public record AplicacaoUpdateRequest(string Titulo, string? Descricao, Guid IdTipoLicenca, bool Ativo);
public record AplicacaoResponse(Guid Id, Guid IdCliente, string Titulo, string? Descricao, Guid IdTipoLicenca, bool Ativo);
