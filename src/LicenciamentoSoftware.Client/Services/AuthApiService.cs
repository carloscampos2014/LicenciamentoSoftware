using System.Net.Http.Json;
using LicenciamentoSoftware.Client.Models.Auth;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>
/// Proxy HTTP para os endpoints de autenticação da API.
/// Usado pelo BFF Server para chamadas server-side.
/// </summary>
public sealed class AuthApiService(HttpClient http)
{
    /// <summary>Primeira etapa do login — retorna AccessToken ou desafio 2FA.</summary>
    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("auth/login", request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
    }

    /// <summary>Segunda etapa — valida código TOTP e retorna tokens definitivos.</summary>
    public async Task<LoginResponse?> VerificarTotpAsync(
        VerificarTotpRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("auth/verify-2fa", request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
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
