namespace LicenciamentoSoftware.Client.Models.Aplicacoes;

public sealed record AtualizarAplicacaoRequest(
    string Titulo,
    string? Descricao);
