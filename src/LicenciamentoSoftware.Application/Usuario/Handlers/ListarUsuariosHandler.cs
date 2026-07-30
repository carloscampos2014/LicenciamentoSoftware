using LicenciamentoSoftware.Application.Common;
using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Queries;
using LicenciamentoSoftware.Application.Usuario.Results;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

public sealed class ListarUsuariosHandler
{
    private readonly IUsuarioGestaoRepository _repo;

    public ListarUsuariosHandler(IUsuarioGestaoRepository repo) => _repo = repo;

    public async Task<PagedResult<UsuarioResult>> HandleAsync(
        ListarUsuariosQuery query,
        CancellationToken ct = default)
        => await _repo.ListarAsync(
            query.IdCliente, query.Nome, query.Ativo,
            query.Pagina, query.TamanhoPagina, ct);
}
