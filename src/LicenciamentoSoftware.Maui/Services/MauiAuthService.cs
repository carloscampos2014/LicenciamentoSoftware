using LicenciamentoSoftware.Client.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LicenciamentoSoftware.Maui.Services;

/// <summary>
/// Gerencia autenticação no MAUI.
/// Access token e refresh token ficam no SecureStorage (Android Keystore / Windows DPAPI).
/// Equivalente ao JwtAuthStateProvider + BffController do Web, mas sem BFF.
/// </summary>
public sealed class MauiAuthService(MauiApiClientFactory factory)
{
    private const string KeyAccess  = "licenciamento_access_token";
    private const string KeyRefresh = "licenciamento_refresh_token";
    private const string KeyNome    = "licenciamento_user_nome";
    private const string KeyPapel   = "licenciamento_user_papel";

    // ── Estado em memória ─────────────────────────────────────────────────────

    public string? AccessToken  { get; private set; }
    public string? Nome         { get; private set; }
    public string? Papel        { get; private set; }
    public bool    Autenticado  => !string.IsNullOrEmpty(AccessToken);

    // ── Inicialização ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tenta restaurar a sessão do SecureStorage ao iniciar o app.
    /// Se o access token estiver expirado, tenta renovar via refresh token.
    /// </summary>
    public async Task<bool> TentarRestaurarSessaoAsync()
    {
        try
        {
            var accessToken  = await SecureStorage.GetAsync(KeyAccess);
            var refreshToken = await SecureStorage.GetAsync(KeyRefresh);

            if (string.IsNullOrEmpty(refreshToken))
                return false;

            // Se o access token ainda é válido, restaura diretamente
            if (!string.IsNullOrEmpty(accessToken) && !TokenExpirado(accessToken))
            {
                await AplicarTokenAsync(accessToken,
                    await SecureStorage.GetAsync(KeyNome) ?? string.Empty,
                    await SecureStorage.GetAsync(KeyPapel) ?? string.Empty);
                return true;
            }

            // Tenta renovar via refresh token
            return await RefreshAsync(refreshToken);
        }
        catch
        {
            return false;
        }
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<LoginResultado> LoginAsync(string email, string senha)
    {
        try
        {
            var resultado = await factory.Auth.LoginAsync(new LoginRequest(email, senha));

            if (!resultado.IsSuccess)
            {
                return resultado.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? new LoginResultado.Erro("E-mail ou senha inválidos.")
                    : new LoginResultado.Erro("Erro ao autenticar. Tente novamente.");
            }

            var response = resultado.Body;

            if (response is null)
                return new LoginResultado.Erro("Resposta inesperada do servidor.");

            if (response.Requer2FA)
                return new LoginResultado.Requer2FA(response.TokenTemporario ?? string.Empty);

            if (response.AccessToken is null)
                return new LoginResultado.Erro("E-mail ou senha inválidos.");

            await AplicarTokenAsync(response.AccessToken,
                response.Nome ?? string.Empty,
                response.Papel ?? string.Empty,
                response.RefreshToken);

            return new LoginResultado.Sucesso();
        }
        catch (HttpRequestException)
        {
            return new LoginResultado.Erro("Não foi possível conectar ao servidor.");
        }
        catch (TaskCanceledException)
        {
            return new LoginResultado.Erro("Tempo de conexão esgotado. Verifique sua internet.");
        }
        catch (Exception ex)
        {
            return new LoginResultado.Erro($"Erro inesperado: {ex.Message}");
        }
    }

    // ── TOTP ──────────────────────────────────────────────────────────────────

    public async Task<LoginResultado> VerificarTotpAsync(string tokenTemporario, string codigo)
    {
        try
        {
            var resultado = await factory.Auth.VerificarTotpAsync(
                new VerificarTotpRequest(tokenTemporario, codigo));

            if (!resultado.IsSuccess)
            {
                return resultado.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? new LoginResultado.Erro("Código inválido ou expirado.")
                    : new LoginResultado.Erro("Erro ao validar código. Tente novamente.");
            }

            var response = resultado.Body;

            if (response?.AccessToken is null)
                return new LoginResultado.Erro("Código inválido ou expirado.");

            await AplicarTokenAsync(response.AccessToken,
                response.Nome ?? string.Empty,
                response.Papel ?? string.Empty,
                response.RefreshToken);

            return new LoginResultado.Sucesso();
        }
        catch (HttpRequestException)
        {
            return new LoginResultado.Erro("Não foi possível conectar ao servidor.");
        }
        catch (TaskCanceledException)
        {
            return new LoginResultado.Erro("Tempo de conexão esgotado. Verifique sua internet.");
        }
        catch (Exception ex)
        {
            return new LoginResultado.Erro($"Erro inesperado: {ex.Message}");
        }
    }

    // ── Refresh silencioso ────────────────────────────────────────────────────

    public async Task<bool> RefreshAsync(string? refreshToken = null)
    {
        try
        {
            refreshToken ??= await SecureStorage.GetAsync(KeyRefresh);
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var response = await factory.Auth.RefreshAsync(refreshToken);
            if (response?.AccessToken is null) return false;

            await AplicarTokenAsync(response.AccessToken,
                response.Nome ?? Nome ?? string.Empty,
                response.Papel ?? Papel ?? string.Empty,
                response.RefreshToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extrai o IdCliente (tenant) do claim <c>id_cliente</c> do JWT em memória.
    /// </summary>
    public Guid? ObterIdCliente()
    {
        if (string.IsNullOrEmpty(AccessToken)) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token   = handler.ReadJwtToken(AccessToken);
            var val     = token.Claims.FirstOrDefault(c => c.Type == "id_cliente")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
        catch { return null; }
    }

    /// <summary>Extrai o IdUsuario do claim <c>sub</c> do JWT em memória.</summary>
    public Guid? ObterIdUsuario()
    {
        if (string.IsNullOrEmpty(AccessToken)) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token   = handler.ReadJwtToken(AccessToken);
            var val     = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
        catch { return null; }
    }

    /// <summary>Extrai o e-mail do claim <c>email</c> do JWT em memória.</summary>
    public string? ObterEmail()
    {
        if (string.IsNullOrEmpty(AccessToken)) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token   = handler.ReadJwtToken(AccessToken);
            return token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        }
        catch { return null; }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.GetAsync(KeyRefresh);
            if (!string.IsNullOrEmpty(refreshToken))
                await factory.Auth.LogoutAsync(refreshToken);
        }
        catch { /* logout é best-effort */ }
        finally
        {
            AccessToken = null;
            Nome = null;
            Papel = null;
            factory.ClearToken();
            SecureStorage.Remove(KeyAccess);
            SecureStorage.Remove(KeyRefresh);
            SecureStorage.Remove(KeyNome);
            SecureStorage.Remove(KeyPapel);
        }
    }

    // ── Auxiliares ────────────────────────────────────────────────────────────

    private async Task AplicarTokenAsync(
        string accessToken, string nome, string papel, string? refreshToken = null)
    {
        AccessToken = accessToken;
        Nome        = nome;
        Papel       = papel;

        factory.SetToken(accessToken);

        await SecureStorage.SetAsync(KeyAccess, accessToken);
        await SecureStorage.SetAsync(KeyNome, nome);
        await SecureStorage.SetAsync(KeyPapel, papel);

        if (refreshToken is not null)
            await SecureStorage.SetAsync(KeyRefresh, refreshToken);
    }

    private static bool TokenExpirado(string jwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token   = handler.ReadJwtToken(jwt);
            return token.ValidTo < DateTime.UtcNow.AddMinutes(-1);
        }
        catch { return true; }
    }
}

// ── Resultados do login ───────────────────────────────────────────────────────

public abstract record LoginResultado
{
    public sealed record Sucesso          : LoginResultado;
    public sealed record Requer2FA(string TokenTemporario) : LoginResultado;
    public sealed record Erro(string Mensagem)             : LoginResultado;
}
