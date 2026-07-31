namespace LicenciamentoSoftware.Client.Models.Aplicacoes;

public sealed record CriarAplicacaoRequest(
    string Titulo,
    string? Descricao,
    Guid IdTipoLicenca);
