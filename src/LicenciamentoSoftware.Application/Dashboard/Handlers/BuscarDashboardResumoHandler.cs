using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Results;

namespace LicenciamentoSoftware.Application.Dashboard.Handlers;

/// <summary>
/// Retorna as métricas gerais do dashboard para o tenant do usuário autenticado.
/// </summary>
public sealed class BuscarDashboardResumoHandler(
    IDashboardRepository repo,
    ICurrentUser currentUser)
{
    public Task<DashboardResumoResult> HandleAsync(CancellationToken ct = default)
        => repo.BuscarResumoAsync(currentUser.IdCliente, ct);
}
