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

        if (!resultado.IsSuccess)
            return Unauthorized(new { Erro = "E-mail ou senha inválidos." });

        var body = resultado.Body;

        if (body is null)
            return Unauthorized(new { Erro = "E-mail ou senha inválidos." });

        if (body.Requer2FA)
            return Ok(new { requer2FA = true, tokenTemporario = body.TokenTemporario });

        if (body.AccessToken is null)
            return Unauthorized(new { Erro = "Falha na autenticação." });

        SetRefreshTokenCookie(body.RefreshToken!, body.Expiracao);

        return Ok(new
        {
            accessToken = body.AccessToken,
            expiracao = body.Expiracao,
            nome = body.Nome,
            papel = body.Papel,
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

        if (!resultado.IsSuccess || resultado.Body?.AccessToken is null)
            return Unauthorized(new { Erro = "Código TOTP inválido ou expirado." });

        var body = resultado.Body;

        SetRefreshTokenCookie(body.RefreshToken!, body.Expiracao);

        return Ok(new
        {
            accessToken = body.AccessToken,
            expiracao = body.Expiracao,
            nome = body.Nome,
            papel = body.Papel,
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

        // Path deve coincidir com o usado na criação do cookie
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            Path = "/bff",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });
        return NoContent();
    }

    // =========================================================================
    // Cadastro público — proxy para a API (sem token, sem cookie)
    // =========================================================================

    /// <summary>
    /// Proxy para POST /auth/cadastrar da API.
    /// O Blazor WASM chama este endpoint do BFF em vez da API diretamente,
    /// evitando necessidade de CORS entre origens diferentes.
    /// IP do cliente é capturado aqui e enviado para a API via AutoCadastroRequest.
    /// </summary>
    [HttpPost("cadastrar")]
    public async Task<IActionResult> Cadastrar(
        [FromBody] AutoCadastroRequest request,
        CancellationToken ct)
    {
        var (sucesso, erro, erros) = await authService.CadastrarAsync(request, ct);

        if (sucesso)
            return Ok(new { Mensagem = "Cadastro realizado com sucesso. Faça login para continuar." });

        if (erros is { Count: > 0 })
            return UnprocessableEntity(new { Erros = erros });

        // CPF/CNPJ ou e-mail duplicado
        return Conflict(new { Erro = erro });
    }

    // =========================================================================
    // Minha Conta — proxy para a API (requer autenticação via JWT no header)
    // =========================================================================

    /// <summary>
    /// Proxy para POST /usuarios/minha-conta/excluir.
    /// O access token é enviado pelo WASM no header Authorization.
    /// </summary>
    [HttpPost("minha-conta/excluir")]
    public async Task<IActionResult> ExcluirMinhaConta(
        [FromBody] ExcluirContaBffRequest request,
        CancellationToken ct)
    {
        var accessToken = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized();

        var (sucesso, erro) = await authService.ExcluirContaAsync(accessToken, request.SenhaAtual, ct);

        if (sucesso) return NoContent();

        if (erro == "Senha incorreta.")
            return Unauthorized(new { Erro = erro });

        return Conflict(new { Erro = erro });
    }

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
            Path = "/bff",
        });
    }
}

public sealed record ExcluirContaBffRequest(string SenhaAtual);
