namespace LicenciamentoSoftware.Api.DTOs;

// TipoLicenca é fixo/global (seed) - somente leitura via API.
public record TipoLicencaResponse(Guid Id, string Descricao);
