using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Auth.Results;using Microsoft.AspNetCore.Authorization;
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
    private readonly ConfirmarTotpHandler _confirmarTotpHandler;
    private readonly DesativarTotpHandler _desativarTotpHandler;
    private readonly AutoCadastrarClienteHandler _autoCadastrarHandler;
    private readonly DefinirSenhaInicialHandler _definirSenhaHandler;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        LoginHandler loginHandler,
        VerificarTotpHandler totpHandler,
        RefreshTokenHandler refreshHandler,
        LogoutHandler logoutHandler,
        RegistrarUsuarioHandler registrarHandler,
        ConfigurarTotpHandler configurarTotpHandler,
        ConfirmarTotpHandler confirmarTotpHandler,
        DesativarTotpHandler desativarTotpHandler,
        AutoCadastrarClienteHandler autoCadastrarHandler,
        DefinirSenhaInicialHandler definirSenhaHandler,
        ICurrentUser currentUser)
    {
        _loginHandler = loginHandler;
        _totpHandler = totpHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _registrarHandler = registrarHandler;
        _configurarTotpHandler = configurarTotpHandler;
        _confirmarTotpHandler = confirmarTotpHandler;
        _desativarTotpHandler = desativarTotpHandler;
        _autoCadastrarHandler = autoCadastrarHandler;
        _definirSenhaHandler = definirSenhaHandler;
        _currentUser = currentUser;
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
            AuthResult.SemSenha sp => Ok(new
            {
                SemSenha = true, TokenTemporario = sp.TokenTemporario,
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
    [AllowAnonymous]
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
    /// Confirma que o autenticador foi configurado corretamente
    /// validando o primeiro código TOTP gerado pelo app do usuário.
    /// </summary>
    [HttpPost("totp/confirmar")]
    [Authorize]
    public async Task<IActionResult> ConfirmarTotp(
        [FromBody] ConfirmarTotpRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _confirmarTotpHandler.HandleAsync(
            new ConfirmarTotpCommand(_currentUser.Id, request.Codigo),
            cancellationToken);

        return resultado switch
        {
            ConfirmarTotpResult.Sucesso        => Ok(new { Mensagem = "2FA confirmado e ativo." }),
            ConfirmarTotpResult.CodigoInvalido => Unauthorized(new { Erro = "Código TOTP inválido ou expirado." }),
            ConfirmarTotpResult.NaoEncontrado  => NotFound(new { Erro = "Usuário não encontrado ou 2FA não configurado." }),
            _                                  => StatusCode(500),
        };
    }

    /// <summary>
    /// Desativa o 2FA TOTP após confirmação com o código atual.
    /// </summary>
    [HttpDelete("totp")]
    [Authorize]
    public async Task<IActionResult> DesativarTotp(
        [FromBody] DesativarTotpRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _desativarTotpHandler.HandleAsync(
            new DesativarTotpCommand(_currentUser.Id, request.CodigoAtual),
            cancellationToken);

        return resultado switch
        {
            DesativarTotpResult.Sucesso        => NoContent(),
            DesativarTotpResult.CodigoInvalido => Unauthorized(new { Erro = "Código TOTP inválido." }),
            DesativarTotpResult.NaoEncontrado  => NotFound(new { Erro = "2FA não está ativo para este usuário." }),
            _                                  => StatusCode(500),
        };
    }

    /// <summary>
    /// Retorna o status do 2FA do usuário autenticado.
    /// </summary>
    [HttpGet("totp/status")]
    [Authorize]
    public async Task<IActionResult> StatusTotp(
        [FromServices] IUsuarioRepository usuarioRepository,
        CancellationToken cancellationToken)
    {
        var sub = User.FindFirst("sub")?.Value
               ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(sub, out var idUsuario))
            return Unauthorized();

        var usuario = await usuarioRepository.BuscarPorIdAsync(idUsuario, cancellationToken);
        if (usuario is null) return NotFound();
        return Ok(new { Ativo = usuario.TotpSecretHash is not null });
    }

    /// <summary>
    /// Define a senha inicial para uma conta anonimizada (sem senha após exclusão LGPD).
    /// Requer o token temporário de papel "DefinirSenha" retornado pelo login quando a conta não tem senha.
    /// Após sucesso, o usuário está autenticado e recebe JWT completo.
    /// </summary>
    [HttpPost("definir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> DefinirSenhaInicial(
        [FromBody] DefinirSenhaInicialRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _definirSenhaHandler.HandleAsync(
            new DefinirSenhaInicialCommand(request.TokenTemporario, request.NovaSenha),
            cancellationToken);

        return resultado switch
        {
            AuthResult.Sucesso s => Ok(new
            {
                s.AccessToken, s.RefreshToken,
                Expiracao = s.Expiracao, s.Nome, s.Papel,
            }),
            AuthResult.TokenInvalido t => Unauthorized(new { Erro = t.Motivo }),
            AuthResult.Negado n        => UnprocessableEntity(new { Erro = n.Motivo }),
            _                          => StatusCode(500),
        };
    }

    /// <summary>
    /// Auto-cadastro público: cria Cliente + primeiro Usuário (AdministradorCliente) em uma transação.
    /// Não requer autenticação.
    /// </summary>    [HttpPost("cadastrar")]
    [AllowAnonymous]
    public async Task<IActionResult> AutoCadastrar(
        [FromBody] AutoCadastrarRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        var resultado = await _autoCadastrarHandler.HandleAsync(
            new AutoCadastrarClienteCommand(
                request.RazaoSocial,
                request.TipoInscricao,
                request.NumeroInscricao,
                request.EmailCliente,
                request.Telefone,
                request.NomeResponsavel,
                request.EmailResponsavel,
                request.Senha,
                request.AceiteLgpd,
                ip),
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

    /// <summary>Altera a própria senha do usuário autenticado.</summary>
    [HttpPut("minha-senha")]
    [Authorize]
    public async Task<IActionResult> AlterarSenha(
        [FromBody] AlterarSenhaRequest request,
        [FromServices] AlterarSenhaHandler handler,
        CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(
            new AlterarSenhaCommand(_currentUser.Id, request.SenhaAtual, request.NovaSenha, request.ConfirmacaoNovaSenha), ct);

        return resultado switch
        {
            AlterarSenhaResult.Sucesso              => NoContent(),
            AlterarSenhaResult.SenhaAtualIncorreta  => Unauthorized(new { Erro = "Senha atual incorreta." }),
            AlterarSenhaResult.UsuarioNaoEncontrado => NotFound(),
            AlterarSenhaResult.Invalido i           => UnprocessableEntity(new { Erros = i.Erros }),
            _                                       => StatusCode(500),
        };
    }

    /// <summary>Inicia o fluxo de recuperação de senha — envia e-mail com link.</summary>
    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> EsqueciSenha(
        [FromBody] EsqueciSenhaRequest request,
        [FromServices] EsqueciSenhaHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(request.Email, ct);
        // Sempre retorna 200 para não vazar se o e-mail existe
        return Ok(new { Mensagem = "Se o e-mail estiver cadastrado, você receberá um link em instantes." });
    }

    /// <summary>Redefine a senha usando o token recebido por e-mail.</summary>
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha(
        [FromBody] RedefinirSenhaRequest request,
        [FromServices] RedefinirSenhaHandler handler,
        CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(
            new RedefinirSenhaCommand(request.Token, request.NovaSenha, request.ConfirmacaoNovaSenha), ct);

        return resultado switch
        {
            RedefinirSenhaResult.Sucesso                 => NoContent(),
            RedefinirSenhaResult.TokenInvalidoOuExpirado => UnprocessableEntity(new { Erro = "Link inválido ou expirado. Solicite um novo link." }),
            RedefinirSenhaResult.Invalido i              => UnprocessableEntity(new { Erros = i.Erros }),
            _                                            => StatusCode(500),
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
public sealed record ConfirmarTotpRequest(string Codigo);
public sealed record DesativarTotpRequest(string CodigoAtual);
public sealed record DefinirSenhaInicialRequest(string TokenTemporario, string NovaSenha);
public sealed record AutoCadastrarRequest(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string EmailCliente,
    string? Telefone,
    string NomeResponsavel,
    string EmailResponsavel,
    string Senha,
    bool AceiteLgpd = false);
public sealed record AlterarSenhaRequest(string SenhaAtual, string NovaSenha, string ConfirmacaoNovaSenha);
public sealed record EsqueciSenhaRequest(string Email);
public sealed record RedefinirSenhaRequest(string Token, string NovaSenha, string ConfirmacaoNovaSenha);
