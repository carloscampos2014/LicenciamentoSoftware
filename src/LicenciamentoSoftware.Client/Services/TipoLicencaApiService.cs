using System.Net.Http.Json;
using LicenciamentoSoftware.Client.Models.TiposLicenca;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>Proxy HTTP para os endpoints de Tipos de Licença da API.</summary>
public sealed class TipoLicencaApiService(HttpClient http)
{
    public async Task<IReadOnlyList<TipoLicencaResult>?> ListarAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<IReadOnlyList<TipoLicencaResult>>("tipos-licenca", ct);

    public async Task<TipoLicencaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<TipoLicencaResult>($"tipos-licenca/{id}", ct);
}
