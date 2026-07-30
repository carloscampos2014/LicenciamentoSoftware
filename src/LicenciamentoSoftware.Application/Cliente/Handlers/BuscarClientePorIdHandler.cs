using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Results;

namespace LicenciamentoSoftware.Application.Cliente.Handlers;

public sealed class BuscarClientePorIdHandler
{
    private readonly IClienteRepository _repo;

    public BuscarClientePorIdHandler(IClienteRepository repo) => _repo = repo;

    public async Task<ClienteResult?> HandleAsync(Guid id, CancellationToken ct = default)
        => await _repo.BuscarPorIdAsync(id, ct);
}
