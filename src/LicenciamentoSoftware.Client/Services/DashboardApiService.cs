using System.Net.Http.Json;
using LicenciamentoSoftware.Client.Models.Dashboard;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>Proxy HTTP para os endpoints de dashboard da API.</summary>
public sealed class DashboardApiService(HttpClient http)
{
    public async Task<DashboardResumoResult?> BuscarResumoAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<DashboardResumoResult>("dashboard/resumo", ct);

    public async Task<DashboardAlertasResult?> BuscarAlertasAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<DashboardAlertasResult>("dashboard/alertas", ct);
}
