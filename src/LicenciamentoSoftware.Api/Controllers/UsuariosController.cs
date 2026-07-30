using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Handlers;
using LicenciamentoSoftware.Application.Usuario.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("usuarios")]
[Authorize(Policy = "AdministradorCliente")]
public sealed class UsuariosController : ControllerBase
{
    private readonly CriarUsuarioHandler _criarHandler;
    private readonly AtualizarUsuarioHandler _atualizarHandler;
    private readonly DesativarUsuarioHandler _desativarHandler;
    private readonly BuscarUsuarioPorIdHandler _buscarHandler;
    private readonly ListarUsuariosHandler _listarHandler;

    public UsuariosController(
        CriarUsuarioHandler criarHandler,
        AtualizarUsuarioHandler atualizarHandler,
        DesativarUsuarioHandler desativarHandler,
        BuscarUsuarioPorIdHandler buscarHandler,
        ListarUsuariosHandler listarHandler)
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
        [FromQuery] string? nome,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.HandleAsync(
            new ListarUsuariosQuery { IdCliente = idCliente, Nome = nome, Ativo = ativo, Pagina = pagina, TamanhoPagina = tamanhoPagina }, ct);
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
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest request, CancellationToken ct)
    {
        var resultado = await _criarHandler.HandleAsync(
            new CriarUsuarioCommand(request.IdCliente, request.Nome, request.Email, request.Senha, request.Papel ?? "OperadorCliente"), ct);

        return resultado switch
        {
            CriarUsuarioResult.Sucesso s      => CreatedAtAction(nameof(BuscarPorId), new { id = s.Usuario.Id }, s.Usuario),
            CriarUsuarioResult.Invalido i     => UnprocessableEntity(new { Erros = i.Erros }),
            CriarUsuarioResult.EmailJaExiste  => Conflict(new { Erro = "E-mail já está em uso." }),
            _                                 => StatusCode(500),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarUsuarioRequest request, CancellationToken ct)
    {
        var resultado = await _atualizarHandler.HandleAsync(
            new AtualizarUsuarioCommand(id, request.Nome, request.Email), ct);

        return resultado switch
        {
            AtualizarUsuarioResult.Sucesso s      => Ok(s.Usuario),
            AtualizarUsuarioResult.Invalido i     => UnprocessableEntity(new { Erros = i.Erros }),
            AtualizarUsuarioResult.NaoEncontrado  => NotFound(),
            AtualizarUsuarioResult.EmailJaExiste  => Conflict(new { Erro = "E-mail já está em uso." }),
            _                                     => StatusCode(500),
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        var resultado = await _desativarHandler.HandleAsync(id, ct);

        return resultado switch
        {
            DesativarUsuarioResult.Sucesso       => NoContent(),
            DesativarUsuarioResult.NaoEncontrado => NotFound(),
            DesativarUsuarioResult.JaInativo     => Conflict(new { Erro = "Usuário já está inativo." }),
            _                                    => StatusCode(500),
        };
    }
}

public sealed record CriarUsuarioRequest(Guid IdCliente, string Nome, string Email, string Senha, string? Papel);
public sealed record AtualizarUsuarioRequest(string Nome, string Email);
