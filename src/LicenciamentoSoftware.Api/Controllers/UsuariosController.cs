using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Handlers;
using LicenciamentoSoftware.Application.Usuario.Queries;
using LicenciamentoSoftware.Application.Usuario.Results;
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
    private readonly ExcluirContaHandler _excluirContaHandler;
    private readonly ICurrentUser _currentUser;

    public UsuariosController(
        CriarUsuarioHandler criarHandler,
        AtualizarUsuarioHandler atualizarHandler,
        DesativarUsuarioHandler desativarHandler,
        BuscarUsuarioPorIdHandler buscarHandler,
        ListarUsuariosHandler listarHandler,
        ExcluirContaHandler excluirContaHandler,
        ICurrentUser currentUser)
    {
        _criarHandler        = criarHandler;
        _atualizarHandler    = atualizarHandler;
        _desativarHandler    = desativarHandler;
        _buscarHandler       = buscarHandler;
        _listarHandler       = listarHandler;
        _excluirContaHandler = excluirContaHandler;
        _currentUser         = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? nome,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.HandleAsync(
            new ListarUsuariosQuery
            {
                IdCliente = _currentUser.IdCliente,
                Nome = nome,
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
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest request, CancellationToken ct)
    {
        // IdCliente sempre do JWT — nunca do body
        var resultado = await _criarHandler.HandleAsync(
            new CriarUsuarioCommand(
                _currentUser.IdCliente,
                request.Nome,
                request.Email,
                request.Senha,
                request.Papel ?? "OperadorCliente"), ct);

        return resultado switch
        {
            CriarUsuarioResult.Sucesso s     => CreatedAtAction(nameof(BuscarPorId), new { id = s.Usuario.Id }, s.Usuario),
            CriarUsuarioResult.Invalido i    => UnprocessableEntity(new { Erros = i.Erros }),
            CriarUsuarioResult.EmailJaExiste => Conflict(new { Erro = "E-mail já está em uso." }),
            _                                => StatusCode(500),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarUsuarioRequest request, CancellationToken ct)
    {
        var resultado = await _atualizarHandler.HandleAsync(
            new AtualizarUsuarioCommand(id, request.Nome, request.Email), ct);

        return resultado switch
        {
            AtualizarUsuarioResult.Sucesso s     => Ok(s.Usuario),
            AtualizarUsuarioResult.Invalido i    => UnprocessableEntity(new { Erros = i.Erros }),
            AtualizarUsuarioResult.NaoEncontrado => NotFound(),
            AtualizarUsuarioResult.EmailJaExiste => Conflict(new { Erro = "E-mail já está em uso." }),
            _                                    => StatusCode(500),
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

    /// <summary>
    /// LGPD Art. 18 — o próprio usuário autenticado solicita a exclusão/anonimização
    /// dos seus dados pessoais. Requer confirmação da senha atual.
    /// </summary>
    [HttpPost("minha-conta/excluir")]
    [Authorize(Policy = "Leitor")] // qualquer usuário autenticado
    public async Task<IActionResult> ExcluirMinhaConta(
        [FromBody] ExcluirContaRequest request,
        CancellationToken ct)
    {
        var resultado = await _excluirContaHandler.HandleAsync(
            new ExcluirContaCommand(
                _currentUser.Id,
                _currentUser.IdCliente,
                request.SenhaAtual), ct);

        return resultado switch
        {
            ExcluirContaResult.Sucesso       => NoContent(),
            ExcluirContaResult.SenhaInvalida => Unauthorized(new { Erro = "Senha incorreta." }),
            ExcluirContaResult.NaoEncontrado => NotFound(),
            _                               => StatusCode(500),
        };
    }
}

public sealed record CriarUsuarioRequest(string Nome, string Email, string Senha, string? Papel);
public sealed record AtualizarUsuarioRequest(string Nome, string Email);
public sealed record ExcluirContaRequest(string SenhaAtual);
