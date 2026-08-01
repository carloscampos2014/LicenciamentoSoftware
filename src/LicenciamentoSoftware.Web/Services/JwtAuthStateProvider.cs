using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace LicenciamentoSoftware.Web.Services;

/// <summary>
/// Gerencia o estado de autenticação do Blazor WASM.
/// O access token fica exclusivamente em memória (campo privado) — nunca toca localStorage ou cookies.
/// O refresh é feito via BFF (/bff/refresh) que gerencia o cookie HttpOnly server-side.
/// </summary>
public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private string? _accessToken;
    private ClaimsPrincipal _usuario = new(new ClaimsIdentity());
    private ApiHttpClientFactory? _apiFactory;

    public string? AccessToken => _accessToken;

    /// <summary>
    /// Injeta a fábrica de clients após a criação para evitar dependência circular.
    /// Chamado pelo Program.cs após registrar os serviços.
    /// </summary>
    public void SetApiFactory(ApiHttpClientFactory factory)
        => _apiFactory = factory;

    /// <summary>
    /// Retorna o estado atual. Chamado automaticamente pelo Blazor em cada render
    /// e ao navegar para páginas protegidas.
    /// </summary>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return Task.FromResult(Anonimo);

        return Task.FromResult(new AuthenticationState(_usuario));
    }

    /// <summary>
    /// Chamado após login bem-sucedido (etapa 1 ou etapa 2 TOTP).
    /// Parseia o JWT, extrai claims e notifica o Blazor.
    /// </summary>
    public void MarcarAutenticado(string accessToken, string nome, string papel)
    {
        _accessToken = accessToken;

        var identity = new ClaimsIdentity(
            claims: ParseClaimsFromJwt(accessToken)
                .Append(new Claim(ClaimTypes.Name, nome))
                .Append(new Claim(ClaimTypes.Role, papel)),
            authenticationType: "jwt");

        _usuario = new ClaimsPrincipal(identity);

        // Atualiza o token em todos os HttpClients autenticados
        _apiFactory?.SetToken(accessToken);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>Atualiza só o access token após refresh silencioso.</summary>
    public void AtualizarToken(string novoAccessToken, string nome, string papel)
        => MarcarAutenticado(novoAccessToken, nome, papel);

    /// <summary>Chamado após logout — limpa o token e notifica o Blazor.</summary>
    public void MarcarDesautenticado()
    {
        _accessToken = null;
        _usuario = new ClaimsPrincipal(new ClaimsIdentity());

        // Remove o token de todos os HttpClients
        _apiFactory?.ClearToken();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonimo));
    }

    public string? ObterPapel()
        => _usuario.FindFirst(ClaimTypes.Role)?.Value;

    public string? ObterNome()
        => _usuario.FindFirst(ClaimTypes.Name)?.Value;

    // -------------------------------------------------------------------------
    // Parseia claims do payload JWT (base64url, sem verificação de assinatura —
    // a assinatura já foi verificada pela API antes de emitir o token)
    // -------------------------------------------------------------------------
    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

        if (keyValuePairs is null) return [];

        return keyValuePairs.Select(kvp =>
            new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty));
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
