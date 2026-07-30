using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Queries;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// Gestão de licenças: emissão, consulta, desativação, tokens HMAC e operações manuais.
/// </summary>
[ApiController]
[Route("licencas")]
[Authorize(Policy = "OperadorCliente")]
public sealed class LicencaController : ControllerBase
{
    // Fase 4 — tokens HMAC
    private readonly EmitirTokenLicencaHandler _emitirTokenHandler;
    private readonly RenovarTokenLicencaHandler _renovarTokenHandler;

    // Fase 6 — CRUD de licença
    private readonly EmitirLicencaHandler _emitirLicencaHandler;
    private readonly BuscarLicencaPorIdHandler _buscarHandler;
    private readonly ListarLicencasHandler _listarHandler;
    private readonly DesativarLicencaHandler _desativarHandler;

    // Fase 6 — operações manuais
    private readonly RenovarPeriodoHandler _renovarPeriodoHandler;
    private readonly EncerrarSessaoHandler _encerrarSessaoHandler;
    private readonly LiberarInstalacaoHandler _liberarInstalacaoHandler;

    public LicencaController(
        EmitirTokenLicencaHandler emitirTokenHandler,
        RenovarTokenLicencaHandler renovarTokenHandler,
        EmitirLicencaHandler emitirLicencaHandler,
        BuscarLicencaPorIdHandler buscarHandler,
        ListarLicencasHandler listarHandler,
        DesativarLicencaHandler desativarHandler,
        RenovarPeriodoHandler renovarPeriodoHandler,
        EncerrarSessaoHandler encerrarSessaoHandler,
        LiberarInstalacaoHandler liberarInstalacaoHandler)
    {
        _emitirTokenHandler       = emitirTokenHandler;
        _renovarTokenHandler      = renovarTokenHandler;
        _emitirLicencaHandler     = emitirLicencaHandler;
        _buscarHandler            = buscarHandler;
        _listarHandler            = listarHandler;
        _desativarHandler         = desativarHandler;
        _renovarPeriodoHandler    = renovarPeriodoHandler;
        _encerrarSessaoHandler    = encerrarSessaoHandler;
        _liberarInstalacaoHandler = liberarInstalacaoHandler;
    }

    // =========================================================================
    // CRUD de licença (Fase 6)
    // =========================================================================

    /// <summary>Lista licenças com filtros e paginação.</summary>
    [HttpGet]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? idCliente,
        [FromQuery] Guid? idClienteFinal,
        [FromQuery] Guid? idAplicativo,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.HandleAsync(
            new ListarLicencasQuery
            {
                IdCliente = idCliente, IdClienteFinal = idClienteFinal,
                IdAplicativo = idAplicativo, Ativo = ativo,
                Pagina = pagina, TamanhoPagina = tamanhoPagina,
            }, ct);
        return Ok(resultado);
    }

    /// <summary>Busca uma licença pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> BuscarPorId(Guid id, CancellationToken ct)
    {
        var resultado = await _buscarHandler.HandleAsync(id, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    /// <summary>
    /// Emite uma nova licença. Informe exatamente um bloco de detalhe compatível
    /// com o TipoLicenca da Aplicação (ou nenhum para licença Permanente).
    /// Use EmitirToken=true para gerar o token HMAC junto com a licença.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdministradorCliente")]
    public async Task<IActionResult> EmitirLicenca(
        [FromBody] EmitirLicencaRequest request,
        CancellationToken ct)
    {
        var resultado = await _emitirLicencaHandler.HandleAsync(
            new EmitirLicencaCommand(
                request.IdClienteFinal, request.IdAplicativo,
                request.Periodo is null ? null : new DetalhePeriodoCommand(
                    request.Periodo.DataInicio, request.Periodo.DataFim, request.Periodo.RenovacaoAutomatica),
                request.Usuarios is null ? null : new DetalheUsuariosCommand(
                    request.Usuarios.QuantidadeMaxima, request.Usuarios.MaxSessoesPorUsuario,
                    request.Usuarios.TempoLimiteSessaoHoras),
                request.Instalacao is null ? null : new DetalheInstalacaoCommand(
                    request.Instalacao.QuantidadeMaxima),
                request.EmitirToken,
                request.ExpiracaoTokenMinutos),
            ct);

        return resultado switch
        {
            EmitirLicencaResult.Sucesso s => CreatedAtAction(nameof(BuscarPorId),
                new { id = s.Licenca.Id },
                new
                {
                    s.Licenca,
                    TokenTexto = s.TokenTexto,
                    Aviso = s.TokenTexto is not null
                        ? "Token exibido uma única vez. Guarde-o com segurança."
                        : null,
                }),
            EmitirLicencaResult.Invalido i                => UnprocessableEntity(new { Erros = i.Erros }),
            EmitirLicencaResult.AcessoNegado              => Forbid(),
            EmitirLicencaResult.ClienteFinalNaoEncontrado => NotFound(new { Erro = "Cliente final não encontrado." }),
            EmitirLicencaResult.AplicacaoNaoEncontrada    => NotFound(new { Erro = "Aplicação não encontrada ou inativa." }),
            EmitirLicencaResult.TipoLicencaIncompativel t => UnprocessableEntity(new { Erro = t.Motivo }),
            EmitirLicencaResult.LicencaDuplicada          => Conflict(new { Erro = "Já existe uma licença ativa para esta combinação cliente final + aplicação." }),
            _                                             => StatusCode(500),
        };
    }

    /// <summary>Desativa uma licença (exclusão lógica).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdministradorCliente")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        var resultado = await _desativarHandler.HandleAsync(id, ct);

        return resultado switch
        {
            DesativarLicencaResult.Sucesso       => NoContent(),
            DesativarLicencaResult.NaoEncontrado => NotFound(),
            DesativarLicencaResult.JaInativo     => Conflict(new { Erro = "Licença já está inativa." }),
            _                                    => StatusCode(500),
        };
    }

    // =========================================================================
    // Operações manuais (Fase 6)
    // =========================================================================

    /// <summary>Renova a data de fim de uma licença Por Período.</summary>
    [HttpPost("{id:guid}/renovar-periodo")]
    [Authorize(Policy = "AdministradorCliente")]
    public async Task<IActionResult> RenovarPeriodo(
        Guid id,
        [FromBody] RenovarPeriodoRequest request,
        CancellationToken ct)
    {
        var resultado = await _renovarPeriodoHandler.HandleAsync(
            new RenovarPeriodoCommand(id, request.NovaDataFim), ct);

        return resultado switch
        {
            RenovarPeriodoResult.Sucesso s         => Ok(new { NovaDataFim = s.NovaDataFim }),
            RenovarPeriodoResult.LicencaNaoEncontrada => NotFound(),
            RenovarPeriodoResult.LicencaInativa    => Conflict(new { Erro = "Licença está inativa." }),
            RenovarPeriodoResult.LicencaSemPeriodo => UnprocessableEntity(new { Erro = "Esta licença não é do tipo Por Período." }),
            RenovarPeriodoResult.DataInvalida d    => UnprocessableEntity(new { Erro = d.Motivo }),
            _                                      => StatusCode(500),
        };
    }

    /// <summary>Encerra manualmente uma sessão ativa.</summary>
    [HttpDelete("{id:guid}/sessoes/{idSessao:guid}")]
    [Authorize(Policy = "AdministradorCliente")]
    public async Task<IActionResult> EncerrarSessao(
        Guid id, Guid idSessao, CancellationToken ct)
    {
        var resultado = await _encerrarSessaoHandler.HandleAsync(idSessao, ct);

        return resultado switch
        {
            EncerrarSessaoResult.Sucesso     => NoContent(),
            EncerrarSessaoResult.NaoEncontrado => NotFound(),
            EncerrarSessaoResult.JaEncerrada => Conflict(new { Erro = "Sessão já está encerrada." }),
            _                                => StatusCode(500),
        };
    }

    /// <summary>Libera manualmente uma instalação registrada.</summary>
    [HttpDelete("{id:guid}/instalacoes/{idInstalacao:guid}")]
    [Authorize(Policy = "AdministradorCliente")]
    public async Task<IActionResult> LiberarInstalacao(
        Guid id, Guid idInstalacao, CancellationToken ct)
    {
        var resultado = await _liberarInstalacaoHandler.HandleAsync(idInstalacao, ct);

        return resultado switch
        {
            LiberarInstalacaoResult.Sucesso      => NoContent(),
            LiberarInstalacaoResult.NaoEncontrado => NotFound(),
            LiberarInstalacaoResult.JaLiberada   => Conflict(new { Erro = "Instalação já está liberada." }),
            _                                    => StatusCode(500),
        };
    }

    // =========================================================================
    // Tokens HMAC (Fase 4 — mantidos)
    // =========================================================================

    /// <summary>
    /// Emite um token HMAC-SHA256 para a licença.
    /// O segredo é retornado UMA ÚNICA VEZ.
    /// </summary>
    [HttpPost("{id:guid}/token")]
    public async Task<IActionResult> EmitirToken(
        Guid id,
        [FromBody] EmitirTokenRequest request,
        CancellationToken ct)
    {
        var resultado = await _emitirTokenHandler.HandleAsync(
            new EmitirTokenLicencaCommand(id, request.ExpiracaoMinutos), ct);

        return resultado switch
        {
            EmitirTokenResult.Sucesso s => CreatedAtAction(
                nameof(EmitirToken), new { id = s.IdToken },
                new { s.IdToken, s.IdLicenca, s.TokenTexto, s.ExpiracaoMinutos,
                      Aviso = "Este é o único momento em que o token é exibido. Guarde-o com segurança." }),
            EmitirTokenResult.LicencaNaoEncontrada => NotFound(new { Erro = "Licença não encontrada." }),
            EmitirTokenResult.LicencaInativa       => UnprocessableEntity(new { Erro = "Licença está inativa." }),
            EmitirTokenResult.TokenJaExiste        => Conflict(new { Erro = "Já existe um token ativo. Use o endpoint de renovação." }),
            _                                      => StatusCode(500),
        };
    }

    /// <summary>Renova o token HMAC revogando o anterior. O novo segredo é retornado UMA ÚNICA VEZ.</summary>
    [HttpPost("{id:guid}/token/renovar")]
    [HttpPost("~/auth/licenca/renovar-token")]
    public async Task<IActionResult> RenovarToken(
        Guid id,
        [FromBody] RenovarTokenRequest request,
        CancellationToken ct)
    {
        var resultado = await _renovarTokenHandler.HandleAsync(
            new RenovarTokenLicencaCommand(id, request.ExpiracaoMinutos), ct);

        return resultado switch
        {
            EmitirTokenResult.Sucesso s => Ok(new
            {
                s.IdToken, s.IdLicenca, s.TokenTexto, s.ExpiracaoMinutos,
                Aviso = "Este é o único momento em que o token é exibido. Guarde-o com segurança.",
            }),
            EmitirTokenResult.LicencaNaoEncontrada => NotFound(new { Erro = "Licença não encontrada." }),
            EmitirTokenResult.LicencaInativa       => UnprocessableEntity(new { Erro = "Licença está inativa." }),
            _                                      => StatusCode(500),
        };
    }
}

// =========================================================================
// Request DTOs
// =========================================================================

public sealed record EmitirLicencaRequest(
    Guid IdClienteFinal,
    Guid IdAplicativo,
    DetalhePeriodoRequest? Periodo,
    DetalheUsuariosRequest? Usuarios,
    DetalheInstalacaoRequest? Instalacao,
    bool EmitirToken = false,
    int? ExpiracaoTokenMinutos = null);

public sealed record DetalhePeriodoRequest(
    DateTime DataInicio, DateTime DataFim, bool RenovacaoAutomatica = false);

public sealed record DetalheUsuariosRequest(
    int QuantidadeMaxima, int MaxSessoesPorUsuario = 5, int TempoLimiteSessaoHoras = 24);

public sealed record DetalheInstalacaoRequest(int QuantidadeMaxima);

public sealed record RenovarPeriodoRequest(DateTime NovaDataFim);

// Fase 4 — mantidos
public sealed record EmitirTokenRequest(int? ExpiracaoMinutos = null);
public sealed record RenovarTokenRequest(int? ExpiracaoMinutos = null);
