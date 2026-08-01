using System.Net;
using System.Net.Http.Json;

namespace LicenciamentoSoftware.Web.Services;

/// <summary>
/// DelegatingHandler que intercepta respostas 401.
/// Chama /bff/refresh (que usa o cookie HttpOnly automaticamente),
/// atualiza o access token em memória e retenta a requisição original.
/// </summary>
public sealed class TokenRefreshHandler : DelegatingHandler
{
    private readonly JwtAuthStateProvider _authProvider;

    // HttpClient sem handler para chamar /bff/refresh (evita loop recursivo)
    private HttpClient? _refreshClient;

    public TokenRefreshHandler(JwtAuthStateProvider authProvider)
    {
        _authProvider = authProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        // Adiciona o Bearer token atual se disponível
        var token = _authProvider.AccessToken;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        // Se não foi 401 ou não temos token para renovar, retorna direto
        if (response.StatusCode != HttpStatusCode.Unauthorized
            || string.IsNullOrEmpty(token))
            return response;

        // Tenta renovar via BFF usando um HttpClient simples (sem este handler)
        _refreshClient ??= new HttpClient
        {
            BaseAddress = new Uri(request.RequestUri?.GetLeftPart(UriPartial.Authority)
                ?? string.Empty)
        };

        var refreshResponse = await _refreshClient.PostAsync("/bff/refresh", null, ct);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            _authProvider.MarcarDesautenticado();
            return response;
        }

        var novoToken = await refreshResponse.Content
            .ReadFromJsonAsync<BffRefreshResponse>(ct);

        if (novoToken?.AccessToken is null)
        {
            _authProvider.MarcarDesautenticado();
            return response;
        }

        _authProvider.AtualizarToken(
            novoToken.AccessToken,
            novoToken.Nome ?? string.Empty,
            novoToken.Papel ?? string.Empty);

        // Retenta a requisição original com o novo token
        var retryRequest = await CloneRequestAsync(request);
        retryRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", novoToken.AccessToken);

        return await base.SendAsync(retryRequest, ct);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content is not null)
        {
            var content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private sealed record BffRefreshResponse(
        string? AccessToken,
        DateTime? Expiracao,
        string? Nome,
        string? Papel);
}
