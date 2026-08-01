using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Results;

namespace LicenciamentoSoftware.Application.Dashboard.Handlers;

/// <summary>
/// Retorna os alertas operacionais do dashboard para o tenant do usuário autenticado.
/// </summary>
public sealed class BuscarDashboardAlertasHandler(
    IDashboardRepository repo,
    ICurrentUser currentUser)
{
    public Task<DashboardAlertasResult> HandleAsync(CancellationToken ct = default)
        => repo.BuscarAlertasAsync(currentUser.IdCliente, ct);
}
