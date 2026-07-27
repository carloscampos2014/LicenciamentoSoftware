using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly LicenciamentoDbContext _db;
    public ClientesController(LicenciamentoDbContext db) => _db = db;

    private static ClienteResponse ToResponse(Cliente c) =>
        new(c.Id, c.RazaoSocial, c.TipoInscricao, c.NumeroInscricao, c.Email, c.Telefone, c.Ativo);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteResponse>>> Listar([FromQuery] bool incluirInativos = false)
    {
        var query = _db.Clientes.AsQueryable();
        if (!incluirInativos) query = query.Where(c => c.Ativo);
        var itens = await query.OrderBy(c => c.RazaoSocial).ToListAsync();
        return Ok(itens.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteResponse>> ObterPorId(Guid id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        return Ok(ToResponse(cliente));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteResponse>> Criar(ClienteCreateRequest request)
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            RazaoSocial = request.RazaoSocial,
            TipoInscricao = request.TipoInscricao,
            NumeroInscricao = request.NumeroInscricao,
            Email = request.Email,
            Telefone = request.Telefone,
            Ativo = true
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, ToResponse(cliente));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteResponse>> Atualizar(Guid id, ClienteUpdateRequest request)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        cliente.RazaoSocial = request.RazaoSocial;
        cliente.TipoInscricao = request.TipoInscricao;
        cliente.NumeroInscricao = request.NumeroInscricao;
        cliente.Email = request.Email;
        cliente.Telefone = request.Telefone;
        cliente.Ativo = request.Ativo;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(cliente));
    }

    // Exclusão lógica - nunca remove o registro fisicamente.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        cliente.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
