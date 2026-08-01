using System.Net.Http.Json;
using System.Web;
using LicenciamentoSoftware.Client.Models.Common;
using LicenciamentoSoftware.Client.Models.Licencas;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>Proxy HTTP para os endpoints de Licenças da API.</summary>
public sealed class LicencaApiService(HttpClient http)
{
    public async Task<PagedResult<LicencaResult>?> ListarAsync(
        Guid? idClienteFinal = null,
        Guid? idAplicativo = null,
        bool? ativo = null,
        int pagina = 1,
        int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (idClienteFinal.HasValue) query["idClienteFinal"] = idClienteFinal.Value.ToString();
        if (idAplicativo.HasValue) query["idAplicativo"] = idAplicativo.Value.ToString();
        if (ativo.HasValue) query["ativo"] = ativo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
        query["pagina"] = pagina.ToString(System.Globalization.CultureInfo.InvariantCulture);
        query["tamanhoPagina"] = tamanhoPagina.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return await http.GetFromJsonAsync<PagedResult<LicencaResult>>(
            $"licencas?{query}", ct);
    }

    public async Task<LicencaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<LicencaResult>($"licencas/{id}", ct);

    public async Task<(bool Sucesso, LicencaResult? Licenca, string? TokenTexto, string? Erro, IReadOnlyList<string>? Erros)> EmitirAsync(
        EmitirLicencaRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("licencas", request, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<EmitirLicencaResponse>(ct);
            return (true, result?.Licenca, result?.TokenTexto, null, null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadFromJsonAsync<ErroResponse>(ct);
            return (false, null, null, body?.Erro ?? "Licença duplicada.", null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var body = await response.Content.ReadFromJsonAsync<ErrosResponse>(ct);
            return (false, null, null, body?.Erro, body?.Erros);
        }

        return (false, null, null, "Erro inesperado.", null);
    }

    public async Task<(bool Sucesso, string? Erro)> DesativarAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"licencas/{id}", ct);
        if (response.IsSuccessStatusCode) return (true, null);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "Licença já está inativa.");
        return (false, "Erro inesperado.");
    }

    public async Task<(bool Sucesso, DateTime? NovaDataFim, string? Erro)> RenovarPeriodoAsync(
        Guid id,
        RenovarPeriodoRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"licencas/{id}/renovar-periodo", request, ct);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<RenovarPeriodoResponse>(ct);
            return (true, body?.NovaDataFim, null);
        }

        var erro = await response.Content.ReadFromJsonAsync<ErroResponse>(ct);
        return (false, null, erro?.Erro ?? "Erro inesperado.");
    }

    public async Task<(bool Sucesso, string? Erro)> EncerrarSessaoAsync(
        Guid idLicenca,
        Guid idSessao,
        CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"licencas/{idLicenca}/sessoes/{idSessao}", ct);
        if (response.IsSuccessStatusCode) return (true, null);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "Sessão já está encerrada.");
        return (false, "Erro inesperado.");
    }

    public async Task<(bool Sucesso, string? Erro)> LiberarInstalacaoAsync(
        Guid idLicenca,
        Guid idInstalacao,
        CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"licencas/{idLicenca}/instalacoes/{idInstalacao}", ct);
        if (response.IsSuccessStatusCode) return (true, null);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "Instalação já está liberada.");
        return (false, "Erro inesperado.");
    }

    public async Task<(bool Sucesso, string? TokenTexto, string? Erro)> GerarTokenAsync(
        Guid idLicenca, int? expiracaoMinutos = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            $"licencas/{idLicenca}/token",
            new { ExpiracaoMinutos = expiracaoMinutos },
            ct);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<TokenEmitidoResponse>(ct);
            return (true, body?.TokenTexto, null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, null, "Já existe um token ativo. Use Renovar Token.");

        var erro = await response.Content.ReadFromJsonAsync<ErroResponse>(ct);
        return (false, null, erro?.Erro ?? "Erro inesperado.");
    }

    public async Task<(bool Sucesso, string? TokenTexto, string? Erro)> RenovarTokenAsync(
        Guid idLicenca, int? expiracaoMinutos = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            $"licencas/{idLicenca}/token/renovar",
            new { ExpiracaoMinutos = expiracaoMinutos },
            ct);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<TokenEmitidoResponse>(ct);
            return (true, body?.TokenTexto, null);
        }

        var erro = await response.Content.ReadFromJsonAsync<ErroResponse>(ct);
        return (false, null, erro?.Erro ?? "Erro inesperado.");
    }

    private sealed record TokenEmitidoResponse(string? TokenTexto);
    private sealed record RenovarPeriodoResponse(DateTime NovaDataFim);

    private sealed record EmitirLicencaResponse(
        LicencaResult Licenca,
        string? TokenTexto,
        string? Aviso);
    private sealed record ErroResponse(string? Erro);
    private sealed record ErrosResponse(string? Erro, IReadOnlyList<string>? Erros);
}
