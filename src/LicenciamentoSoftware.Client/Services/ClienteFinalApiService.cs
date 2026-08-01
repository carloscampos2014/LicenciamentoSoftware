using System.Net.Http.Json;
using System.Web;
using LicenciamentoSoftware.Client.Models.ClientesFinais;
using LicenciamentoSoftware.Client.Models.Common;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>Proxy HTTP para os endpoints de Clientes Finais da API.</summary>
public sealed class ClienteFinalApiService(HttpClient http)
{
    public async Task<PagedResult<ClienteFinalResult>?> ListarAsync(
        string? razaoSocial = null,
        bool? ativo = null,
        int pagina = 1,
        int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (razaoSocial is not null) query["razaoSocial"] = razaoSocial;
        if (ativo.HasValue) query["ativo"] = ativo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
        query["pagina"] = pagina.ToString(System.Globalization.CultureInfo.InvariantCulture);
        query["tamanhoPagina"] = tamanhoPagina.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return await http.GetFromJsonAsync<PagedResult<ClienteFinalResult>>(
            $"clientes-finais?{query}", ct);
    }

    public async Task<ClienteFinalResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<ClienteFinalResult>($"clientes-finais/{id}", ct);

    public async Task<(bool Sucesso, ClienteFinalResult? Result, string? Erro, IReadOnlyList<string>? Erros)> CriarAsync(
        CriarClienteFinalRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("clientes-finais", request, ct);
        return await ParseWriteResponseAsync<ClienteFinalResult>(response, ct);
    }

    public async Task<(bool Sucesso, ClienteFinalResult? Result, string? Erro, IReadOnlyList<string>? Erros)> AtualizarAsync(
        Guid id,
        AtualizarClienteFinalRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"clientes-finais/{id}", request, ct);
        return await ParseWriteResponseAsync<ClienteFinalResult>(response, ct);
    }

    public async Task<(bool Sucesso, string? Erro)> DesativarAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"clientes-finais/{id}", ct);
        if (response.IsSuccessStatusCode) return (true, null);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "Cliente final já está inativo.");
        return (false, "Erro inesperado.");
    }

    private static async Task<(bool Sucesso, T? Result, string? Erro, IReadOnlyList<string>? Erros)>
        ParseWriteResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<T>(ct);
            return (true, result, null, null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadFromJsonAsync<ErroResponse>(ct);
            return (false, default, body?.Erro ?? "Conflito.", null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var body = await response.Content.ReadFromJsonAsync<ErrosResponse>(ct);
            return (false, default, null, body?.Erros);
        }

        return (false, default, "Erro inesperado.", null);
    }

    private sealed record ErroResponse(string Erro);
    private sealed record ErrosResponse(IReadOnlyList<string> Erros);
}
