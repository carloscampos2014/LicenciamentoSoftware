using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// Endpoints de validação de licença — chamados pelos softwares dos clientes finais.
/// <para>
/// Autenticação: <b>HMAC-SHA256</b> via headers <c>X-Signature</c>, <c>X-Timestamp</c> e <c>X-Nonce</c>.
/// Não usa JWT Bearer. O middleware anti-replay (<c>AntiReplayMiddleware</c>) protege todos os
/// endpoints deste controller automaticamente via <c>UseWhen(/api/validacao)</c>.
/// </para>
/// </summary>
[ApiController]
[Route("api/validacao")]
[EnableRateLimiting("validacao")]
public sealed class ValidacaoController : ControllerBase
{
    private readonly ValidarLoginHandler _validarLoginHandler;
    private readonly HeartbeatHandler _heartbeatHandler;
    private readonly LogoutValidacaoHandler _logoutHandler;
    private readonly ValidarInstalacaoHandler _validarInstalacaoHandler;
    private readonly IHmacLicencaTokenService _hmac;
    private readonly ILicencaTokenRepository _tokenRepo;
    private readonly ILicencaRepository _licencaRepo;
    private readonly IClienteRepository _clienteRepo;

    private static readonly JsonSerializerOptions HmacJsonOpts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        WriteIndented          = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ValidacaoController(
        ValidarLoginHandler validarLoginHandler,
        HeartbeatHandler heartbeatHandler,
        LogoutValidacaoHandler logoutHandler,
        ValidarInstalacaoHandler validarInstalacaoHandler,
        IHmacLicencaTokenService hmac,
        ILicencaTokenRepository tokenRepo,
        ILicencaRepository licencaRepo,
        IClienteRepository clienteRepo)
    {
        _validarLoginHandler      = validarLoginHandler;
        _heartbeatHandler         = heartbeatHandler;
        _logoutHandler            = logoutHandler;
        _validarInstalacaoHandler = validarInstalacaoHandler;
        _hmac                     = hmac;
        _tokenRepo                = tokenRepo;
        _licencaRepo              = licencaRepo;
        _clienteRepo              = clienteRepo;
    }

    // =========================================================================
    // POST /api/validacao/login
    // =========================================================================

    /// <summary>
    /// Valida o acesso de um usuário a uma licença.
    /// Suporta os tipos Permanente, Por Período e Por Usuários.
    /// Para licenças Por Instalação, use POST /api/validacao/instalacao.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> ValidarLogin(
        [FromBody] ValidarLoginRequest request,
        CancellationToken ct)
    {
        var hmacOk = await VerificarHmacAsync(
            request.IdLicenca, body: System.Text.Json.JsonSerializer.Serialize(request, HmacJsonOpts), ct);
        if (!hmacOk)
            return Unauthorized(new { Erro = "Assinatura HMAC inválida ou token de licença não encontrado." });

        var resultado = await _validarLoginHandler.HandleAsync(
            new ValidarLoginCommand(request.IdLicenca, request.IdentificadorUsuario,
                HttpContext.Connection.RemoteIpAddress?.ToString()), ct);

        return resultado switch
        {
            ValidarLoginResult.Sucesso s =>
                Ok(new { Autorizado = true, IdSessao = s.IdSessao }),

            ValidarLoginResult.Invalido i =>
                UnprocessableEntity(new { Erros = i.Erros }),

            ValidarLoginResult.LicencaNaoEncontrada =>
                NotFound(new { Erro = "Licença não encontrada." }),

            ValidarLoginResult.LicencaInativa =>
                UnprocessableEntity(new { Erro = "Licença inativa." }),

            ValidarLoginResult.LicencaExpirada =>
                UnprocessableEntity(new { Erro = "Licença expirada. Renove o período para continuar." }),

            ValidarLoginResult.LimiteUsuariosAtingido l =>
                StatusCode(StatusCodes.Status429TooManyRequests,
                    new { Erro = $"Limite de {l.QuantidadeMaxima} usuário(s) simultâneo(s) atingido." }),

            ValidarLoginResult.LimiteSessionsPorUsuarioAtingido l =>
                StatusCode(StatusCodes.Status429TooManyRequests,
                    new { Erro = $"Limite de {l.MaxSessoesPorUsuario} sessão(ões) por usuário atingido." }),

            ValidarLoginResult.TipoLicencaIncompativel t =>
                UnprocessableEntity(new { Erro = t.Motivo }),

            _ => StatusCode(500),
        };
    }

    // =========================================================================
    // POST /api/validacao/heartbeat
    // =========================================================================

    /// <summary>
    /// Registra atividade em uma sessão ativa (keep-alive).
    /// Deve ser chamado periodicamente pelo software cliente para evitar
    /// encerramento automático por inatividade.
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(
        [FromBody] HeartbeatRequest request,
        CancellationToken ct)
    {
        var hmacOk = await VerificarHmacAsync(
            request.IdLicenca, body: System.Text.Json.JsonSerializer.Serialize(request, HmacJsonOpts), ct);
        if (!hmacOk)
            return Unauthorized(new { Erro = "Assinatura HMAC inválida ou token de licença não encontrado." });

        var resultado = await _heartbeatHandler.HandleAsync(
            new HeartbeatCommand(request.IdLicenca, request.IdSessao,
                HttpContext.Connection.RemoteIpAddress?.ToString()), ct);

        return resultado switch
        {
            HeartbeatResult.Sucesso          => NoContent(),
            HeartbeatResult.SessaoNaoEncontrada => NotFound(new { Erro = "Sessão não encontrada." }),
            HeartbeatResult.SessaoEncerrada  => UnprocessableEntity(new { Erro = "Sessão encerrada." }),
            HeartbeatResult.AcessoNegado     => Forbid(),
            _                                => StatusCode(500),
        };
    }

    // =========================================================================
    // POST /api/validacao/logout
    // =========================================================================

    /// <summary>
    /// Encerra explicitamente uma sessão de validação.
    /// Operação idempotente: sessão já encerrada retorna 204 sem erro.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutValidacaoRequest request,
        CancellationToken ct)
    {
        var hmacOk = await VerificarHmacAsync(
            request.IdLicenca, body: System.Text.Json.JsonSerializer.Serialize(request, HmacJsonOpts), ct);
        if (!hmacOk)
            return Unauthorized(new { Erro = "Assinatura HMAC inválida ou token de licença não encontrado." });

        var resultado = await _logoutHandler.HandleAsync(
            new LogoutValidacaoCommand(request.IdLicenca, request.IdSessao,
                HttpContext.Connection.RemoteIpAddress?.ToString()), ct);

        return resultado switch
        {
            LogoutValidacaoResult.Sucesso            => NoContent(),
            LogoutValidacaoResult.SessaoNaoEncontrada => NotFound(new { Erro = "Sessão não encontrada." }),
            LogoutValidacaoResult.AcessoNegado       => Forbid(),
            _                                        => StatusCode(500),
        };
    }

    // =========================================================================
    // POST /api/validacao/instalacao
    // =========================================================================

    /// <summary>
    /// Valida e registra a instalação de um software em uma máquina.
    /// Exclusivo para licenças do tipo Por Instalação.
    /// Operação idempotente: máquina já registrada retorna 200 com JaRegistrada=true.
    /// </summary>
    [HttpPost("instalacao")]
    public async Task<IActionResult> ValidarInstalacao(
        [FromBody] ValidarInstalacaoRequest request,
        CancellationToken ct)
    {
        var hmacOk = await VerificarHmacAsync(
            request.IdLicenca, body: System.Text.Json.JsonSerializer.Serialize(request, HmacJsonOpts), ct);
        if (!hmacOk)
            return Unauthorized(new { Erro = "Assinatura HMAC inválida ou token de licença não encontrado." });

        var resultado = await _validarInstalacaoHandler.HandleAsync(
            new ValidarInstalacaoCommand(request.IdLicenca, request.IdentificadorMaquina,
                HttpContext.Connection.RemoteIpAddress?.ToString()), ct);

        return resultado switch
        {
            ValidarInstalacaoResult.Sucesso s =>
                Ok(new { Autorizado = true, IdInstalacao = s.IdInstalacao, JaRegistrada = s.JaRegistrada }),

            ValidarInstalacaoResult.Invalido i =>
                UnprocessableEntity(new { Erros = i.Erros }),

            ValidarInstalacaoResult.LicencaNaoEncontrada =>
                NotFound(new { Erro = "Licença não encontrada." }),

            ValidarInstalacaoResult.LicencaInativa =>
                UnprocessableEntity(new { Erro = "Licença inativa." }),

            ValidarInstalacaoResult.LicencaExpirada =>
                UnprocessableEntity(new { Erro = "Licença expirada. Renove o período para continuar." }),

            ValidarInstalacaoResult.LimiteInstalacoesAtingido l =>
                StatusCode(StatusCodes.Status429TooManyRequests,
                    new { Erro = $"Limite de {l.QuantidadeMaxima} instalação(ões) atingido." }),

            ValidarInstalacaoResult.TipoLicencaIncompativel t =>
                UnprocessableEntity(new { Erro = t.Motivo }),

            _ => StatusCode(500),
        };
    }

    // =========================================================================
    // Helper privado — verificação HMAC
    // =========================================================================

    /// <summary>
    /// Verifica a autenticidade da requisição em dois passos:
    /// <list type="number">
    ///   <item>
    ///     Confirma que o <c>X-Token</c> (segredo em texto puro enviado pelo cliente)
    ///     corresponde ao hash BCrypt armazenado para a licença — via
    ///     <see cref="IHmacLicencaTokenService.VerificarHashSegredo"/>.
    ///   </item>
    ///   <item>
    ///     Valida a assinatura HMAC-SHA256 do <c>X-Signature</c> calculada sobre
    ///     <c>{idLicenca}:{X-Timestamp}:{body}</c> usando o segredo em texto puro.
    ///   </item>
    /// </list>
    /// Headers obrigatórios: <c>X-Token</c>, <c>X-Timestamp</c>, <c>X-Signature</c>, <c>X-Nonce</c>.
    /// (<c>X-Timestamp</c> e <c>X-Nonce</c> já foram validados pelo AntiReplayMiddleware.)
    /// </summary>
    private async Task<bool> VerificarHmacAsync(
        Guid idLicenca, string body, CancellationToken ct)
    {
        // X-Token: segredo em texto puro fornecido pelo cliente
        if (!Request.Headers.TryGetValue("X-Token", out var tokenRaw)
            || string.IsNullOrWhiteSpace(tokenRaw))
            return false;

        // X-Timestamp: já validado pelo AntiReplayMiddleware mas precisamos para o HMAC
        if (!Request.Headers.TryGetValue("X-Timestamp", out var timestampRaw)
            || string.IsNullOrWhiteSpace(timestampRaw))
            return false;

        // X-Signature: assinatura HMAC-SHA256 calculada pelo cliente
        if (!Request.Headers.TryGetValue("X-Signature", out var signatureRaw)
            || string.IsNullOrWhiteSpace(signatureRaw))
            return false;

        // Busca o token ativo da licença (contém o hash BCrypt do segredo)
        var tokenInfo = await _tokenRepo.BuscarAtivoporLicencaAsync(idLicenca, ct);
        if (tokenInfo is null)
            return false;

        // Fase 12.1 — bloqueia validação se a empresa (tenant) estiver encerrada/inativa
        var licenca = await _licencaRepo.BuscarPorIdAsync(idLicenca, ct);
        if (licenca is not null)
        {
            var cliente = await _clienteRepo.BuscarPorIdAsync(licenca.IdCliente, ct);
            if (cliente is not null && !cliente.Ativo)
                return false;
        }

        var segredoTexto = tokenRaw.ToString();

        // Passo 1: confirma que o segredo pertence à licença (BCrypt verify)
        if (!_hmac.VerificarHashSegredo(segredoTexto, tokenInfo.SegredoHash))
            return false;

        // Passo 2: valida a assinatura HMAC-SHA256
        return _hmac.ValidarAssinatura(
            idLicenca,
            body,
            timestampRaw.ToString(),
            segredoTexto,
            signatureRaw.ToString());
    }
}

// =========================================================================
// Request DTOs
// =========================================================================

public sealed record ValidarLoginRequest(
    Guid IdLicenca,
    string IdentificadorUsuario,
    string Assinatura = "");

public sealed record HeartbeatRequest(
    Guid IdLicenca,
    Guid IdSessao);

public sealed record LogoutValidacaoRequest(
    Guid IdLicenca,
    Guid IdSessao);

public sealed record ValidarInstalacaoRequest(
    Guid IdLicenca,
    string IdentificadorMaquina);
