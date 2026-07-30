using LicenciamentoSoftware.Application.Common;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Queries;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class ListarLicencasHandler
{
    private readonly ILicencaGestaoRepository _repo;

    public ListarLicencasHandler(ILicencaGestaoRepository repo) => _repo = repo;

    public async Task<PagedResult<LicencaResult>> HandleAsync(
        ListarLicencasQuery query, CancellationToken ct = default)
        => await _repo.ListarAsync(
            query.IdCliente, query.IdClienteFinal, query.IdAplicativo,
            query.Ativo, query.Pagina, query.TamanhoPagina, ct);
}
