namespace LicenciamentoSoftware.Client.Models.Aplicacoes;

public sealed record AplicacaoResult(
    Guid Id,
    Guid IdCliente,
    string Titulo,
    string? Descricao,
    Guid IdTipoLicenca,
    string TipoLicencaDescricao,
    bool Ativo);
