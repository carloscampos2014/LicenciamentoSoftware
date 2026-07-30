using LicenciamentoSoftware.Application.ClienteFinal.Commands;
using LicenciamentoSoftware.Application.ClienteFinal.Handlers;
using LicenciamentoSoftware.Application.ClienteFinal.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("clientes-finais")]
[Authorize(Policy = "AdministradorCliente")]
public sealed class ClientesFinaisController : ControllerBase
{
    private readonly CriarClienteFinalHandler _criarHandler;
    private readonly AtualizarClienteFinalHandler _atualizarHandler;
    private readonly DesativarClienteFinalHandler _desativarHandler;
    private readonly BuscarClienteFinalPorIdHandler _buscarHandler;
    private readonly ListarClientesFinaisHandler _listarHandler;

    public ClientesFinaisController(
        CriarClienteFinalHandler criarHandler,
        AtualizarClienteFinalHandler atualizarHandler,
        DesativarClienteFinalHandler desativarHandler,
        BuscarClienteFinalPorIdHandler buscarHandler,
        ListarClientesFinaisHandler listarHandler)
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
        [FromQuery] string? razaoSocial,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.HandleAsync(
            new ListarClientesFinaisQuery { IdCliente = idCliente, RazaoSocial = razaoSocial, Ativo = ativo, Pagina = pagina, TamanhoPagina = tamanhoPagina }, ct);
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
    public async Task<IActionResult> Criar([FromBody] CriarClienteFinalRequest request, CancellationToken ct)
    {
        var resultado = await _criarHandler.HandleAsync(
            new CriarClienteFinalCommand(request.IdCliente, request.RazaoSocial,
                request.TipoInscricao, request.NumeroInscricao, request.Email, request.Telefone), ct);

        return resultado switch
        {
            CriarClienteFinalResult.Sucesso s         => CreatedAtAction(nameof(BuscarPorId), new { id = s.ClienteFinal.Id }, s.ClienteFinal),
            CriarClienteFinalResult.Invalido i        => UnprocessableEntity(new { Erros = i.Erros }),
            CriarClienteFinalResult.InscricaoJaExiste => Conflict(new { Erro = "Inscrição (CPF/CNPJ) já cadastrada para este cliente." }),
            _                                         => StatusCode(500),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarClienteFinalRequest request, CancellationToken ct)
    {
        var resultado = await _atualizarHandler.HandleAsync(
            new AtualizarClienteFinalCommand(id, request.RazaoSocial, request.Email, request.Telefone), ct);

        return resultado switch
        {
            AtualizarClienteFinalResult.Sucesso s     => Ok(s.ClienteFinal),
            AtualizarClienteFinalResult.Invalido i    => UnprocessableEntity(new { Erros = i.Erros }),
            AtualizarClienteFinalResult.NaoEncontrado => NotFound(),
            _                                         => StatusCode(500),
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        var resultado = await _desativarHandler.HandleAsync(id, ct);

        return resultado switch
        {
            DesativarClienteFinalResult.Sucesso       => NoContent(),
            DesativarClienteFinalResult.NaoEncontrado => NotFound(),
            DesativarClienteFinalResult.JaInativo     => Conflict(new { Erro = "Cliente final já está inativo." }),
            _                                         => StatusCode(500),
        };
    }
}

public sealed record CriarClienteFinalRequest(
    Guid IdCliente, string RazaoSocial, int TipoInscricao,
    string NumeroInscricao, string Email, string? Telefone);

public sealed record AtualizarClienteFinalRequest(string RazaoSocial, string Email, string? Telefone);
