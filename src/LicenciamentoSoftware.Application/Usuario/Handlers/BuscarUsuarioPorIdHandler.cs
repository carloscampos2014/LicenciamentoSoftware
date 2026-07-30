using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Results;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

public sealed class BuscarUsuarioPorIdHandler
{
    private readonly IUsuarioGestaoRepository _repo;

    public BuscarUsuarioPorIdHandler(IUsuarioGestaoRepository repo) => _repo = repo;

    public async Task<UsuarioResult?> HandleAsync(Guid id, CancellationToken ct = default)
        => await _repo.BuscarPorIdAsync(id, ct);
}
