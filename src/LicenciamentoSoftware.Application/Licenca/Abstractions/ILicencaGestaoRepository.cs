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

    // Edição de detalhes (Fase 10 — issue #219)
    Task AtualizarDetalhesUsuariosAsync(Guid idLicenca, int quantidadeMaxima, int maxSessoesPorUsuario, CancellationToken ct = default);
    Task AtualizarDetalhesInstalacaoAsync(Guid idLicenca, int quantidadeMaxima, CancellationToken ct = default);
    Task AtualizarRenovacaoAutomaticaAsync(Guid idLicenca, bool renovacaoAutomatica, CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // Fase 8 — jobs de expiração, renovação automática e notificação
    // -------------------------------------------------------------------------

    /// <summary>Busca licenças Por Período ativas cujo DataFim já passou (candidatas a expirar).</summary>
    Task<IReadOnlyList<Jobs.LicencaPeriodoJobInfo>> BuscarLicencasPeriodoVencidasAsync(
        DateTime agora, CancellationToken ct = default);

    /// <summary>
    /// Busca licenças Por Período com RenovacaoAutomatica=true cujo DataFim está dentro
    /// da janela de antecedência configurada.
    /// </summary>
    Task<IReadOnlyList<Jobs.LicencaPeriodoJobInfo>> BuscarLicencasRenovacaoAutomaticaAsync(
        DateTime agora, int diasAntecedencia, CancellationToken ct = default);

    /// <summary>Desativa em lote licenças Por Período vencidas sem renovação automática.</summary>
    Task DesativarLicencasPeriodoVencidasAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>Estende o DataFim de uma licença Por Período pelo número de dias informado.</summary>
    Task RenovarDataFimLicencaAsync(
        Guid idLicenca, DateTime novaDataFim, CancellationToken ct = default);

    /// <summary>
    /// Busca licenças Por Período ativas cujo DataFim está próximo do vencimento,
    /// para envio de notificação ao administrador.
    /// </summary>
    Task<IReadOnlyList<Jobs.LicencaPeriodoJobInfo>> BuscarLicencasProximasVencimentoAsync(
        DateTime agora, int diasAntecedencia, CancellationToken ct = default);
}
