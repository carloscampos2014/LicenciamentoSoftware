namespace LicenciamentoSoftware.Application.Aplicacao.Commands;

public sealed record CriarAplicacaoCommand(
    Guid IdCliente,
    string Titulo,
    Guid IdTipoLicenca,
    string? Descricao);
