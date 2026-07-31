using LicenciamentoSoftware.Client.Models.Auth;
using LicenciamentoSoftware.Client.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Web.Server.Controllers;

/// <summary>
/// BFF (Backend for Frontend) — gerencia cookies HttpOnly para o Blazor WASM.
/// Todos os endpoints são anônimos do ponto de vista do BFF;
/// a autenticação real ocorre na API downstream.
/// </summary>
[ApiController]
[Route("bff")]
public sealed class BffController(AuthApiService authService) : ControllerBase
{
    private const string RefreshTokenCookie = "X-Refresh-Token";

    // =========================================================================
    // Login — etapa 1
    // =========================================================================

    /// <summary>
    /// Autentica com e-mail e senha.
    /// Se bem-sucedido: emite cookie HttpOnly com refresh token e retorna access token no body.
    /// Se 2FA necessário: retorna { requer2FA: true, tokenTemporario }.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var resultado = await authService.LoginAsync(request, ct);

        if (resultado is null)
            return Unauthorized(new { Erro = "E-mail ou senha inválidos." });

        if (resultado.Requer2FA)
            return Ok(new { requer2FA = true, tokenTemporario = resultado.TokenTemporario });

        if (resultado.AccessToken is null)
            return Unauthorized(new { Erro = "Falha na autenticação." });

        SetRefreshTokenCookie(resultado.RefreshToken!, resultado.Expiracao);

        return Ok(new
        {
            accessToken = resultado.AccessToken,
            expiracao = resultado.Expiracao,
            nome = resultado.Nome,
            papel = resultado.Papel,
        });
    }

    // =========================================================================
    // Login — etapa 2 (TOTP)
    // =========================================================================

    /// <summary>
    /// Valida o código TOTP da segunda etapa.
    /// Se bem-sucedido: emite cookie HttpOnly e retorna access token.
    /// </summary>
    [HttpPost("login/2fa")]
    public async Task<IActionResult> VerificarTotp(
        [FromBody] VerificarTotpRequest request,
        CancellationToken ct)
    {
        var resultado = await authService.VerificarTotpAsync(request, ct);

        if (resultado is null || resultado.AccessToken is null)
            return Unauthorized(new { Erro = "Código TOTP inválido ou expirado." });

        SetRefreshTokenCookie(resultado.RefreshToken!, resultado.Expiracao);

        return Ok(new
        {
            accessToken = resultado.AccessToken,
            expiracao = resultado.Expiracao,
            nome = resultado.Nome,
            papel = resultado.Papel,
        });
    }

    // =========================================================================
    // Refresh
    // =========================================================================

    /// <summary>
    /// Renova o access token usando o cookie HttpOnly de refresh.
    /// Chamado automaticamente pelo TokenRefreshHandler do WASM quando recebe 401.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { Erro = "Sessão expirada. Faça login novamente." });

        var resultado = await authService.RefreshAsync(refreshToken, ct);

        if (resultado is null || resultado.AccessToken is null)
        {
            // Refresh inválido — apaga o cookie e força novo login
            Response.Cookies.Delete(RefreshTokenCookie);
            return Unauthorized(new { Erro = "Sessão expirada. Faça login novamente." });
        }

        SetRefreshTokenCookie(resultado.RefreshToken!, resultado.Expiracao);

        return Ok(new
        {
            accessToken = resultado.AccessToken,
            expiracao = resultado.Expiracao,
            nome = resultado.Nome,
            papel = resultado.Papel,
        });
    }

    // =========================================================================
    // Logout
    // =========================================================================

    /// <summary>
    /// Encerra a sessão: revoga o refresh token na API e apaga o cookie.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];

        if (!string.IsNullOrEmpty(refreshToken))
            await authService.LogoutAsync(refreshToken, ct);

        Response.Cookies.Delete(RefreshTokenCookie);
        return NoContent();
    }

    // =========================================================================
    // Auxiliar
    // =========================================================================

    private void SetRefreshTokenCookie(string refreshToken, DateTime? expiracao)
    {
        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiracao.HasValue
                ? new DateTimeOffset(expiracao.Value)
                : DateTimeOffset.UtcNow.AddDays(7),
            Path = "/bff", // cookie só enviado para endpoints BFF
        });
    }
}
