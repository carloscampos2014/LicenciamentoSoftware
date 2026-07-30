namespace LicenciamentoSoftware.Application.Aplicacao.Commands;

public sealed record AtualizarAplicacaoCommand(
    Guid Id,
    string Titulo,
    string? Descricao);
