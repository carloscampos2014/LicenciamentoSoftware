using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Handlers;
using LicenciamentoSoftware.Application.Cliente.Queries;
using LicenciamentoSoftware.Application.Cliente.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// CRUD de clientes (empresas que contratam o sistema de licenciamento).
/// </summary>
[ApiController]
[Route("clientes")]
[Authorize(Policy = "AdministradorCliente")]
public sealed class ClientesController : ControllerBase
{
    private readonly CriarClienteHandler _criarHandler;
    private readonly AtualizarClienteHandler _atualizarHandler;
    private readonly DesativarClienteHandler _desativarHandler;
    private readonly BuscarClientePorIdHandler _buscarHandler;
    private readonly ListarClientesHandler _listarHandler;
    private readonly EncerrarContaEmpresaHandler _encerrarHandler;
    private readonly ICurrentUser _currentUser;

    public ClientesController(
        CriarClienteHandler criarHandler,
        AtualizarClienteHandler atualizarHandler,
        DesativarClienteHandler desativarHandler,
        BuscarClientePorIdHandler buscarHandler,
        ListarClientesHandler listarHandler,
        EncerrarContaEmpresaHandler encerrarHandler,
        ICurrentUser currentUser)
    {
        _criarHandler    = criarHandler;
        _atualizarHandler = atualizarHandler;
        _desativarHandler = desativarHandler;
        _buscarHandler   = buscarHandler;
        _listarHandler   = listarHandler;
        _encerrarHandler = encerrarHandler;
        _currentUser     = currentUser;
    }

    /// <summary>Lista clientes com filtro e paginação.</summary>
    [HttpGet]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? razaoSocial,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.HandleAsync(
            new ListarClientesQuery { RazaoSocial = razaoSocial, Ativo = ativo, Pagina = pagina, TamanhoPagina = tamanhoPagina },
            ct);
        return Ok(resultado);
    }

    /// <summary>Busca um cliente pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Leitor")]
    public async Task<IActionResult> BuscarPorId(Guid id, CancellationToken ct)
    {
        var resultado = await _buscarHandler.HandleAsync(id, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    /// <summary>Cria um novo cliente.</summary>
    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarClienteRequest request,
        CancellationToken ct)
    {
        var resultado = await _criarHandler.HandleAsync(
            new CriarClienteCommand(
                request.RazaoSocial, request.TipoInscricao,
                request.NumeroInscricao, request.Email, request.Telefone),
            ct);

        return resultado switch
        {
            CriarClienteResult.Sucesso s          => CreatedAtAction(nameof(BuscarPorId), new { id = s.Cliente.Id }, s.Cliente),
            CriarClienteResult.Invalido i          => UnprocessableEntity(new { Erros = i.Erros }),
            CriarClienteResult.InscricaoJaExiste   => Conflict(new { Erro = "Inscrição (CPF/CNPJ) já cadastrada." }),
            _                                      => StatusCode(500),
        };
    }

    /// <summary>Atualiza dados de um cliente existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarClienteRequest request,
        CancellationToken ct)
    {
        var resultado = await _atualizarHandler.HandleAsync(
            new AtualizarClienteCommand(id, request.RazaoSocial, request.Email, request.Telefone),
            ct);

        return resultado switch
        {
            AtualizarClienteResult.Sucesso s  => Ok(s.Cliente),
            AtualizarClienteResult.Invalido i  => UnprocessableEntity(new { Erros = i.Erros }),
            AtualizarClienteResult.NaoEncontrado => NotFound(),
            _                                  => StatusCode(500),
        };
    }

    /// <summary>Desativa um cliente (exclusão lógica).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        var resultado = await _desativarHandler.HandleAsync(id, ct);

        return resultado switch
        {
            DesativarClienteResult.Sucesso       => NoContent(),
            DesativarClienteResult.NaoEncontrado => NotFound(),
            DesativarClienteResult.JaInativo     => Conflict(new { Erro = "Cliente já está inativo." }),
            _                                    => StatusCode(500),
        };
    }

    /// <summary>
    /// Encerra a conta da empresa do tenant autenticado.
    /// Requer confirmação de senha. A conta é desativada imediatamente e os dados
    /// são excluídos fisicamente em 90 dias (ou imediatamente se ExclusaoImediata = true).
    /// </summary>
    [HttpPost("{id:guid}/encerrar")]
    public async Task<IActionResult> EncerrarConta(
        Guid id,
        [FromBody] EncerrarContaEmpresaRequest request,
        CancellationToken ct)
    {
        // Garante que o AdministradorCliente só pode encerrar o próprio tenant
        if (_currentUser.IdCliente != id)
            return Forbid();

        var resultado = await _encerrarHandler.HandleAsync(
            new EncerrarContaEmpresaCommand(
                IdCliente:        _currentUser.IdCliente,
                IdUsuario:        _currentUser.Id,
                SenhaAtual:       request.SenhaAtual,
                ExclusaoImediata: request.ExclusaoImediata),
            ct);

        return resultado switch
        {
            EncerrarContaEmpresaResult.Sucesso       => NoContent(),
            EncerrarContaEmpresaResult.NaoEncontrado => NotFound(),
            EncerrarContaEmpresaResult.SenhaInvalida => Unauthorized(new { Erro = "Senha incorreta." }),
            EncerrarContaEmpresaResult.JaEncerrada   => Conflict(new { Erro = "Conta já foi encerrada." }),
            _                                        => StatusCode(500),
        };
    }
}

// ----- Request DTOs -----
public sealed record CriarClienteRequest(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string Email,
    string? Telefone);

public sealed record AtualizarClienteRequest(
    string RazaoSocial,
    string Email,
    string? Telefone);

public sealed record EncerrarContaEmpresaRequest(
    string SenhaAtual,
    bool ExclusaoImediata = false);
