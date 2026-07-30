using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class BuscarLicencaPorIdHandler
{
    private readonly ILicencaGestaoRepository _repo;

    public BuscarLicencaPorIdHandler(ILicencaGestaoRepository repo) => _repo = repo;

    public async Task<LicencaResult?> HandleAsync(Guid id, CancellationToken ct = default)
        => await _repo.BuscarPorIdAsync(id, ct);
}
