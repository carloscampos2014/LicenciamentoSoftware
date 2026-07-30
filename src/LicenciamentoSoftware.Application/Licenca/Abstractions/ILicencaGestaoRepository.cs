using LicenciamentoSoftware.Application.Common;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

public interface ILicencaGestaoRepository
{
    Task<LicencaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteLicencaAtivaAsync(
        Guid idCliente, Guid idClienteFinal, Guid idAplicativo,
        CancellationToken ct = default);

    Task<PagedResult<LicencaResult>> ListarAsync(
        Guid? idCliente, Guid? idClienteFinal, Guid? idAplicativo,
        bool? ativo, int pagina, int tamanhoPagina,
        CancellationToken ct = default);

    Task<Guid> InserirLicencaAsync(
        Domain.Entities.Licenca licenca,
        CancellationToken ct = default);

    Task InserirDetalhePeriodoAsync(
        Domain.Entities.LicencaPeriodo periodo,
        CancellationToken ct = default);

    Task InserirDetalheUsuariosAsync(
        Domain.Entities.LicencaUsuarios usuarios,
        CancellationToken ct = default);

    Task InserirDetalheInstalacaoAsync(
        Domain.Entities.LicencaInstalacao instalacao,
        CancellationToken ct = default);

    Task DesativarAsync(Guid id, CancellationToken ct = default);

    // Renovação de período
    Task<DetalhePeriodoResult?> BuscarPeriodoPorLicencaAsync(Guid idLicenca, CancellationToken ct = default);
    Task AtualizarDataFimPeriodoAsync(Guid idLicenca, DateTime novaDataFim, CancellationToken ct = default);
}
