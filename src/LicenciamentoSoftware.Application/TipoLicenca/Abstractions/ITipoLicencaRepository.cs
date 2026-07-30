using LicenciamentoSoftware.Application.TipoLicenca.Results;

namespace LicenciamentoSoftware.Application.TipoLicenca.Abstractions;

public interface ITipoLicencaRepository
{
    Task<IReadOnlyList<TipoLicencaResult>> ListarAsync(CancellationToken ct = default);
    Task<TipoLicencaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
}
