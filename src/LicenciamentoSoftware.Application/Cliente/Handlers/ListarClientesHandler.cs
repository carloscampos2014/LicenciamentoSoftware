using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Queries;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Cliente.Handlers;

public sealed class ListarClientesHandler
{
    private readonly IClienteRepository _repo;

    public ListarClientesHandler(IClienteRepository repo) => _repo = repo;

    public async Task<PagedResult<ClienteResult>> HandleAsync(
        ListarClientesQuery query,
        CancellationToken ct = default)
        => await _repo.ListarAsync(
            query.RazaoSocial,
            query.Ativo,
            query.Pagina,
            query.TamanhoPagina,
            ct);
}
