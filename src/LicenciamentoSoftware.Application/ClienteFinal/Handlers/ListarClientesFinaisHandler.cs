using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Queries;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.ClienteFinal.Handlers;

public sealed class ListarClientesFinaisHandler
{
    private readonly IClienteFinalRepository _repo;

    public ListarClientesFinaisHandler(IClienteFinalRepository repo) => _repo = repo;

    public async Task<PagedResult<ClienteFinalResult>> HandleAsync(
        ListarClientesFinaisQuery query, CancellationToken ct = default)
        => await _repo.ListarAsync(
            query.IdCliente, query.RazaoSocial, query.Ativo,
            query.Pagina, query.TamanhoPagina, ct);
}
