using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

public interface ILicencaSessaoRepository
{
    Task<SessaoResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SessaoResult>> ListarPorLicencaAsync(Guid idLicenca, CancellationToken ct = default);
    Task EncerrarAsync(Guid id, CancellationToken ct = default);
}
