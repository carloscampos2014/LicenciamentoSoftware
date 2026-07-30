using LicenciamentoSoftware.Application.TipoLicenca.Abstractions;
using LicenciamentoSoftware.Application.TipoLicenca.Results;

namespace LicenciamentoSoftware.Application.TipoLicenca.Handlers;

public sealed class ListarTiposLicencaHandler
{
    private readonly ITipoLicencaRepository _repo;

    public ListarTiposLicencaHandler(ITipoLicencaRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TipoLicencaResult>> HandleAsync(CancellationToken ct = default)
        => await _repo.ListarAsync(ct);
}
