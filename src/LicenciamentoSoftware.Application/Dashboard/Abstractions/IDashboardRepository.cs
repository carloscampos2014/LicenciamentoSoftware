using LicenciamentoSoftware.Application.Dashboard.Results;

namespace LicenciamentoSoftware.Application.Dashboard.Abstractions;

/// <summary>
/// Porta de leitura para as métricas do dashboard.
/// Implementada na Infrastructure com queries SQL otimizadas (CTEs).
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Retorna todas as métricas gerais do tenant em uma única roundtrip ao banco.
    /// </summary>
    Task<DashboardResumoResult> BuscarResumoAsync(
        Guid idCliente,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna os alertas operacionais do tenant.
    /// </summary>
    Task<DashboardAlertasResult> BuscarAlertasAsync(
        Guid idCliente,
        CancellationToken ct = default);
}
