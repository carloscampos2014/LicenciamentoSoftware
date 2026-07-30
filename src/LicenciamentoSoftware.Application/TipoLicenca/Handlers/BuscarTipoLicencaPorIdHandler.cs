using LicenciamentoSoftware.Application.TipoLicenca.Abstractions;
using LicenciamentoSoftware.Application.TipoLicenca.Results;

namespace LicenciamentoSoftware.Application.TipoLicenca.Handlers;

public sealed class BuscarTipoLicencaPorIdHandler
{
    private readonly ITipoLicencaRepository _repo;

    public BuscarTipoLicencaPorIdHandler(ITipoLicencaRepository repo) => _repo = repo;

    public async Task<TipoLicencaResult?> HandleAsync(Guid id, CancellationToken ct = default)
        => await _repo.BuscarPorIdAsync(id, ct);
}
