using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Results;

namespace LicenciamentoSoftware.Application.ClienteFinal.Handlers;

public sealed class BuscarClienteFinalPorIdHandler
{
    private readonly IClienteFinalRepository _repo;

    public BuscarClienteFinalPorIdHandler(IClienteFinalRepository repo) => _repo = repo;

    public async Task<ClienteFinalResult?> HandleAsync(Guid id, CancellationToken ct = default)
        => await _repo.BuscarPorIdAsync(id, ct);
}
