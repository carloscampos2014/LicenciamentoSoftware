using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Queries;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Aplicacao.Handlers;

public sealed class ListarAplicacoesHandler
{
    private readonly IAplicacaoRepository _repo;

    public ListarAplicacoesHandler(IAplicacaoRepository repo) => _repo = repo;

    public async Task<PagedResult<AplicacaoResult>> HandleAsync(
        ListarAplicacoesQuery query, CancellationToken ct = default)
        => await _repo.ListarAsync(
            query.IdCliente, query.Titulo, query.Ativo,
            query.Pagina, query.TamanhoPagina, ct);
}
