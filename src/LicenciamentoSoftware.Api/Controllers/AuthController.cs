using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Auth.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// Endpoints de autenticação e gestão de identidade.
/// Não contém regra de negócio — delega aos handlers da Application.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly VerificarTotpHandler _totpHandler;
    private readonly RefreshTokenHandler _refreshHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly RegistrarUsuarioHandler _registrarHandler;
    private readonly ConfigurarTotpHandler _configurarTotpHandler;
    private readonly AutoCadastrarClienteHandler _autoCadastrarHandler;

    public AuthController(
        LoginHandler loginHandler,
        VerificarTotpHandler totpHandler,
        RefreshTokenHandler refreshHandler,
        LogoutHandler logoutHandler,
        RegistrarUsuarioHandler registrarHandler,
        ConfigurarTotpHandler configurarTotpHandler,
        AutoCadastrarClienteHandler autoCadastrarHandler)
    {
        _loginHandler = loginHandler;
        _totpHandler = totpHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _registrarHandler = registrarHandler;
        _configurarTotpHandler = configurarTotpHandler;
        _autoCadastrarHandler = autoCadastrarHandler;
    }

    /// <summary>Registra um novo usuário vinculado a um cliente.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _registrarHandler.HandleAsync(
            new RegistrarUsuarioCommand(
                request.IdCliente, request.Nome, request.Email, request.Senha),
            cancellationToken);

        return resultado switch
        {
            RegistrarResult.Sucesso s => CreatedAtAction(
                nameof(Registrar), new { id = s.IdUsuario },
                new { s.IdUsuario, s.Nome, s.Papel }),
            RegistrarResult.EmailJaEmUso => Conflict(new { Erro = "E-mail já está em uso." }),
            RegistrarResult.ClienteNaoEncontrado => NotFound(new { Erro = "Cliente não encontrado." }),
            _ => StatusCode(500),
        };
    }

    /// <summary>Autentica com e-mail e senha. Retorna JWT ou desafio 2FA.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Senha),
            cancellationToken);

        return resultado switch
        {
            AuthResult.Sucesso s => Ok(new
            {
                s.AccessToken, s.RefreshToken,
                Expiracao = s.Expiracao, s.Nome, s.Papel,
            }),
            AuthResult.Requer2FA r => Ok(new
            {
                Requer2FA = true, TokenTemporario = r.TokenTemporario,
            }),
            AuthResult.Negado n => Unauthorized(new { Erro = n.Motivo }),
            _ => StatusCode(500),
        };
    }

    /// <summary>Segunda etapa do login com código TOTP.</summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerificarTotp(
        [FromBody] VerificarTotpRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _totpHandler.HandleAsync(
            new VerificarTotpCommand(request.TokenTemporario, request.Codigo),
            cancellationToken);

        return resultado switch
        {
            AuthResult.Sucesso s => Ok(new
            {
                s.AccessToken, s.RefreshToken,
                Expiracao = s.Expiracao, s.Nome, s.Papel,
            }),
            AuthResult.TotpInvalido t => Unauthorized(new { Erro = t.Motivo }),
            _ => StatusCode(500),
        };
    }

    /// <summary>Renova o par de tokens usando o refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _refreshHandler.HandleAsync(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return resultado switch
        {
            AuthResult.Sucesso s => Ok(new
            {
                s.AccessToken, s.RefreshToken,
                Expiracao = s.Expiracao, s.Nome, s.Papel,
            }),
            AuthResult.TokenInvalido t => Unauthorized(new { Erro = t.Motivo }),
            _ => StatusCode(500),
        };
    }

    /// <summary>Encerra a sessão revogando o refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _logoutHandler.HandleAsync(
            new LogoutCommand(request.RefreshToken), cancellationToken);

        return NoContent();
    }

    /// <summary>Configura o 2FA TOTP para o usuário autenticado.</summary>
    [HttpPost("totp/setup")]
    [Authorize]
    public async Task<IActionResult> ConfigurarTotp(
        [FromBody] ConfigurarTotpRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _configurarTotpHandler.HandleAsync(
            new ConfigurarTotpCommand(request.IdUsuario, request.Email),
            cancellationToken);

        if (resultado is null)
            return NotFound(new { Erro = "Usuário não encontrado." });

        return Ok(new { resultado.Segredo, resultado.QrCodeUri });
    }

    /// <summary>
    /// Auto-cadastro público: cria Cliente + primeiro Usuário (AdministradorCliente) em uma transação.
    /// Não requer autenticação.
    /// </summary>
    [HttpPost("cadastrar")]
    [AllowAnonymous]
    public async Task<IActionResult> AutoCadastrar(
        [FromBody] AutoCadastrarRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _autoCadastrarHandler.HandleAsync(
            new AutoCadastrarClienteCommand(
                request.RazaoSocial,
                request.TipoInscricao,
                request.NumeroInscricao,
                request.EmailCliente,
                request.Telefone,
                request.NomeResponsavel,
                request.EmailResponsavel,
                request.Senha),
            cancellationToken);

        return resultado switch
        {
            AutoCadastrarClienteResult.Sucesso s => CreatedAtAction(
                nameof(AutoCadastrar),
                new { id = s.IdCliente },
                new { s.IdCliente, s.IdUsuario,
                      Mensagem = "Cadastro realizado com sucesso. Faça login para continuar." }),
            AutoCadastrarClienteResult.Invalido i         => UnprocessableEntity(new { Erros = i.Erros }),
            AutoCadastrarClienteResult.InscricaoJaExiste  => Conflict(new { Erro = "CPF/CNPJ já cadastrado." }),
            AutoCadastrarClienteResult.EmailJaEmUso       => Conflict(new { Erro = "E-mail do responsável já está em uso." }),
            _                                             => StatusCode(500),
        };
    }
}

// ----- Request DTOs (locais ao controller — sem namespace separado para manter simples) -----

public sealed record LoginRequest(string Email, string Senha);
public sealed record VerificarTotpRequest(string TokenTemporario, string Codigo);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record RegistrarUsuarioRequest(Guid IdCliente, string Nome, string Email, string Senha);
public sealed record ConfigurarTotpRequest(Guid IdUsuario, string Email);
public sealed record AutoCadastrarRequest(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string EmailCliente,
    string? Telefone,
    string NomeResponsavel,
    string EmailResponsavel,
    string Senha);
