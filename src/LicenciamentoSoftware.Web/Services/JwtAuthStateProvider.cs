using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace LicenciamentoSoftware.Web.Services;

/// <summary>
/// Gerencia o estado de autenticação do Blazor WASM.
/// O access token fica exclusivamente em memória (campo privado) — nunca toca localStorage ou cookies.
/// Na inicialização (após refresh da página), tenta restaurar a sessão via /bff/refresh usando o
/// cookie HttpOnly gerenciado pelo BFF server-side.
/// </summary>
public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly HttpClient _bffClient;
    private string? _accessToken;
    private ClaimsPrincipal _usuario = new(new ClaimsIdentity());
    private ApiHttpClientFactory? _apiFactory;
    private bool _inicializado;

    public string? AccessToken => _accessToken;

    public JwtAuthStateProvider(IHttpClientFactory httpClientFactory)
    {
        // Client simples sem handler de autenticação — usado exclusivamente para /bff/refresh
        _bffClient = httpClientFactory.CreateClient("bff");
    }

    /// <summary>
    /// Injeta a fábrica de clients após a criação para evitar dependência circular.
    /// Chamado pelo Program.cs após registrar os serviços.
    /// </summary>
    public void SetApiFactory(ApiHttpClientFactory factory)
        => _apiFactory = factory;

    /// <summary>
    /// Retorna o estado de autenticação atual.
    /// Na primeira chamada (após refresh da página), tenta restaurar sessão via /bff/refresh.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Se já tem token em memória, está autenticado
        if (!string.IsNullOrEmpty(_accessToken))
            return new AuthenticationState(_usuario);

        // Na primeira chamada após inicialização/refresh, tenta restaurar via cookie
        if (!_inicializado)
        {
            _inicializado = true;
            await TentarRestaurarSessaoAsync();
        }

        if (string.IsNullOrEmpty(_accessToken))
            return Anonimo;

        return new AuthenticationState(_usuario);
    }

    /// <summary>
    /// Tenta restaurar a sessão chamando /bff/refresh com o cookie HttpOnly.
    /// Se o cookie for válido, restaura o access token silenciosamente.
    /// Se não for, o usuário permanece anônimo e será redirecionado para /login.
    /// </summary>
    private async Task TentarRestaurarSessaoAsync()
    {
        try
        {
            var response = await _bffClient.PostAsync("/bff/refresh", null);
            if (!response.IsSuccessStatusCode) return;

            var resultado = await response.Content
                .ReadFromJsonAsync<BffRefreshResponse>();

            if (resultado?.AccessToken is null) return;

            // Restaura sessão silenciosamente sem notificar (evita loop)
            _accessToken = resultado.AccessToken;

            var identity = new ClaimsIdentity(
                claims: ParseClaimsFromJwt(resultado.AccessToken)
                    .Append(new Claim(ClaimTypes.Name, resultado.Nome ?? string.Empty))
                    .Append(new Claim(ClaimTypes.Role, resultado.Papel ?? string.Empty)),
                authenticationType: "jwt");

            _usuario = new ClaimsPrincipal(identity);
            _apiFactory?.SetToken(resultado.AccessToken);
        }
        catch
        {
            // Sessão não restaurável — usuário fará login novamente
        }
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

    private sealed record BffRefreshResponse(
        string? AccessToken,
        DateTime? Expiracao,
        string? Nome,
        string? Papel);
}
