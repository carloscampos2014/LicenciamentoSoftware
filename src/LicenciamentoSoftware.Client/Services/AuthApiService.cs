using System.Net;
using System.Net.Http.Json;
using LicenciamentoSoftware.Client.Models.Auth;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>
/// Resultado tipado de uma chamada de autenticação.
/// Carrega o StatusCode HTTP junto com o body deserializado,
/// permitindo ao chamador distinguir 401 (credenciais inválidas)
/// de falha de rede (HttpRequestException).
/// </summary>
public sealed record LoginApiResult(HttpStatusCode StatusCode, LoginResponse? Body)
{
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
}

/// <summary>
/// Proxy HTTP para os endpoints de autenticação da API.
/// Usado pelo BFF Server para chamadas server-side.
/// </summary>
public sealed class AuthApiService(HttpClient http)
{
    /// <summary>Primeira etapa do login — retorna AccessToken ou desafio 2FA.</summary>
    public async Task<LoginApiResult> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("auth/login", request, ct);

        if (!response.IsSuccessStatusCode)
            return new LoginApiResult(response.StatusCode, null);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
        return new LoginApiResult(response.StatusCode, body);
    }

    /// <summary>Segunda etapa — valida código TOTP e retorna tokens definitivos.</summary>
    public async Task<LoginApiResult> VerificarTotpAsync(
        VerificarTotpRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("auth/verify-2fa", request, ct);

        if (!response.IsSuccessStatusCode)
            return new LoginApiResult(response.StatusCode, null);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
        return new LoginApiResult(response.StatusCode, body);
    }

    /// <summary>Renova o par de tokens usando o refresh token.</summary>
    public async Task<LoginResponse?> RefreshAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "auth/refresh", new { RefreshToken = refreshToken }, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
    }

    /// <summary>Revoga o refresh token no servidor.</summary>
    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        await http.PostAsJsonAsync("auth/logout", new { RefreshToken = refreshToken }, ct);
    }

    /// <summary>Auto-cadastro: cria Cliente + primeiro Usuário (AdministradorCliente) em uma transação.</summary>
    public async Task<(bool Sucesso, string? Erro, IReadOnlyList<string>? Erros)> CadastrarAsync(
        AutoCadastroRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("auth/cadastrar", request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null, null);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, "CPF/CNPJ ou e-mail já cadastrado.", null);

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var body = await response.Content
                .ReadFromJsonAsync<ErrosResponse>(ct);
            return (false, null, body?.Erros);
        }

        return (false, "Erro inesperado. Tente novamente.", null);
    }

    private sealed record ErrosResponse(IReadOnlyList<string> Erros);
}
