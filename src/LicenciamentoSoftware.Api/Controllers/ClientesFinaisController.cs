using LicenciamentoSoftware.Application.Abstractions;
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
    private readonly ICurrentUser _currentUser;

    public ClientesFinaisController(
        CriarClienteFinalHandler criarHandler,
        AtualizarClienteFinalHandler atualizarHandler,
        DesativarClienteFinalHandler desativarHandler,
        BuscarClienteFinalPorIdHandler buscarHandler,
        ListarClientesFinaisHandler listarHandler,
        ICurrentUser currentUser)
    {
        _criarHandler     = criarHandler;
        _atualizarHandler = atualizarHandler;
        _desativarHandler = desativarHandler;
        _buscarHandler    = buscarHandler;
        _listarHandler    = listarHandler;
        _currentUser      = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? razaoSocial,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        // IdCliente sempre do JWT — tenant isolation
        var resultado = await _listarHandler.HandleAsync(
            new ListarClientesFinaisQuery
            {
                IdCliente = _currentUser.IdCliente,
                RazaoSocial = razaoSocial,
                Ativo = ativo,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            }, ct);
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
        // IdCliente sempre do JWT — nunca do body
        var resultado = await _criarHandler.HandleAsync(
            new CriarClienteFinalCommand(
                _currentUser.IdCliente,
                request.RazaoSocial,
                request.TipoInscricao,
                request.NumeroInscricao,
                request.Email,
                request.Telefone), ct);

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
    string RazaoSocial, int TipoInscricao,
    string NumeroInscricao, string Email, string? Telefone);

public sealed record AtualizarClienteFinalRequest(string RazaoSocial, string Email, string? Telefone);
