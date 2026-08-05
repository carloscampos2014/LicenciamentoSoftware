using System.Net.Http.Json;
using LicenciamentoSoftware.Client.Models.Clientes;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>Proxy HTTP para os endpoints de Clientes da API.</summary>
public sealed class ClienteApiService(HttpClient http)
{
    public async Task<ClienteResult?> BuscarPorIdAsync(
        Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<ClienteResult>($"clientes/{id}", ct);

    /// <summary>
    /// Atualiza os dados da empresa. Retorna (true, null) em caso de sucesso.
    /// </summary>
    public async Task<(bool Sucesso, string? Erro)> AtualizarAsync(
        Guid id,
        AtualizarClienteRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"clientes/{id}", request, ct);

        if (response.IsSuccessStatusCode) return (true, null);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "CPF/CNPJ ou e-mail já está em uso por outra empresa.");

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var body = await response.Content
                .ReadFromJsonAsync<ErrosResponse>(ct);
            return (false, body?.Erros is { Count: > 0 }
                ? string.Join(" ", body.Erros)
                : "Dados inválidos.");
        }

        return (false, "Erro inesperado. Tente novamente.");
    }

    /// <summary>
    /// Encerra a conta da empresa. Requer confirmação de senha.
    /// Se <paramref name="request"/>.ExclusaoImediata = true, os dados são excluídos na próxima
    /// execução do job de limpeza (até 24h). Caso contrário, em 90 dias.
    /// </summary>
    public async Task<(bool Sucesso, string? Erro)> EncerrarContaAsync(
        Guid id,
        EncerrarContaRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"clientes/{id}/encerrar", request, ct);
        return await ParseEncerrarResponseAsync(response, ct);
    }

    /// <summary>
    /// Overload para o BFF: permite passar um access token explícito sem modificar o HttpClient compartilhado.
    /// Usado pelo BffController que recebe o token do WASM via Authorization header.
    /// </summary>
    public async Task<(bool Sucesso, string? Erro)> EncerrarContaAsync(
        Guid id,
        EncerrarContaRequest request,
        string accessToken,
        CancellationToken ct = default)
    {
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"clientes/{id}/encerrar")
        {
            Content = JsonContent.Create(request),
        };
        reqMsg.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.SendAsync(reqMsg, ct);
        return await ParseEncerrarResponseAsync(response, ct);
    }

    private static async Task<(bool Sucesso, string? Erro)> ParseEncerrarResponseAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return (true, null);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (false, "Senha incorreta.");

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadFromJsonAsync<ErroResponse>(ct);
            return (false, body?.Erro ?? "Operação não permitida.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return (false, "Acesso negado.");

        return (false, "Erro inesperado. Tente novamente.");
    }

    private sealed record ErrosResponse(IReadOnlyList<string> Erros);
    private sealed record ErroResponse(string Erro);
}
