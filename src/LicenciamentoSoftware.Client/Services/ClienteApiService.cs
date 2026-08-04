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

    private sealed record ErrosResponse(IReadOnlyList<string> Erros);
}
