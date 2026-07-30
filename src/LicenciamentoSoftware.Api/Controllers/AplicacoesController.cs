using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Handlers;
using LicenciamentoSoftware.Application.Aplicacao.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("aplicacoes")]
[Authorize(Policy = "AdministradorCliente")]
public sealed class AplicacoesController : ControllerBase
{
    private readonly CriarAplicacaoHandler _criarHandler;
    private readonly AtualizarAplicacaoHandler _atualizarHandler;
    private readonly DesativarAplicacaoHandler _desativarHandler;
    private readonly BuscarAplicacaoPorIdHandler _buscarHandler;
    private readonly ListarAplicacoesHandler _listarHandler;

    public AplicacoesController(
        CriarAplicacaoHandler criarHandler,
        AtualizarAplicacaoHandler atualizarHandler,
        DesativarAplicacaoHandler desativarHandler,
        BuscarAplicacaoPorIdHandler buscarHandler,
        ListarAplicacoesHandler listarHandler)
    {
        _criarHandler    = criarHandler;
        _atualizarHandler = atualizarHandler;
        _desativarHandler = desativarHandler;
        _buscarHandler   = buscarHandler;
        _listarHandler   = listarHandler;
    }

    [HttpGet]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? idCliente,
        [FromQuery] string? titulo,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.HandleAsync(
            new ListarAplicacoesQuery { IdCliente = idCliente, Titulo = titulo, Ativo = ativo, Pagina = pagina, TamanhoPagina = tamanhoPagina }, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> BuscarPorId(Guid id, CancellationToken ct)
    {
        var resultado = await _buscarHandler.HandleAsync(id, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarAplicacaoRequest request, CancellationToken ct)
    {
        var resultado = await _criarHandler.HandleAsync(
            new CriarAplicacaoCommand(request.IdCliente, request.Titulo, request.IdTipoLicenca, request.Descricao), ct);

        return resultado switch
        {
            CriarAplicacaoResult.Sucesso s                => CreatedAtAction(nameof(BuscarPorId), new { id = s.Aplicacao.Id }, s.Aplicacao),
            CriarAplicacaoResult.Invalido i               => UnprocessableEntity(new { Erros = i.Erros }),
            CriarAplicacaoResult.TipoLicencaNaoEncontrado => UnprocessableEntity(new { Erro = "Tipo de licença não encontrado." }),
            _                                             => StatusCode(500),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarAplicacaoRequest request, CancellationToken ct)
    {
        var resultado = await _atualizarHandler.HandleAsync(
            new AtualizarAplicacaoCommand(id, request.Titulo, request.Descricao), ct);

        return resultado switch
        {
            AtualizarAplicacaoResult.Sucesso s    => Ok(s.Aplicacao),
            AtualizarAplicacaoResult.Invalido i   => UnprocessableEntity(new { Erros = i.Erros }),
            AtualizarAplicacaoResult.NaoEncontrado => NotFound(),
            _                                     => StatusCode(500),
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        var resultado = await _desativarHandler.HandleAsync(id, ct);

        return resultado switch
        {
            DesativarAplicacaoResult.Sucesso       => NoContent(),
            DesativarAplicacaoResult.NaoEncontrado => NotFound(),
            DesativarAplicacaoResult.JaInativo     => Conflict(new { Erro = "Aplicação já está inativa." }),
            _                                      => StatusCode(500),
        };
    }
}

public sealed record CriarAplicacaoRequest(Guid IdCliente, string Titulo, Guid IdTipoLicenca, string? Descricao);
public sealed record AtualizarAplicacaoRequest(string Titulo, string? Descricao);
