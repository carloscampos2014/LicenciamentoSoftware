using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

public interface ILicencaInstalacaoRepository
{
    Task<InstalacaoRegistradaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<InstalacaoRegistradaResult>> ListarPorLicencaAsync(Guid idLicenca, CancellationToken ct = default);
    Task LiberarAsync(Guid id, CancellationToken ct = default);
}
