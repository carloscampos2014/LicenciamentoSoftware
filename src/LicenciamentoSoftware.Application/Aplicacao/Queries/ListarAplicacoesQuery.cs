using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Aplicacao.Queries;

public sealed record ListarAplicacoesQuery : PagedQuery
{
    public Guid? IdCliente { get; init; }
    public string? Titulo { get; init; }
    public bool? Ativo { get; init; }
}
