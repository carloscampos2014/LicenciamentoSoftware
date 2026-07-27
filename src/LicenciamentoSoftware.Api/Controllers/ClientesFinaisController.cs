using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("api/clientes-finais")]
public class ClientesFinaisController : ControllerBase
{
    private readonly LicenciamentoDbContext _db;
    public ClientesFinaisController(LicenciamentoDbContext db) => _db = db;

    private static ClienteFinalResponse ToResponse(ClienteFinal c) =>
        new(c.Id, c.IdCliente, c.RazaoSocial, c.TipoInscricao, c.NumeroInscricao, c.Email, c.Telefone, c.Ativo);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteFinalResponse>>> Listar([FromQuery] Guid? idCliente, [FromQuery] bool incluirInativos = false)
    {
        var query = _db.ClientesFinais.AsQueryable();
        if (idCliente.HasValue) query = query.Where(c => c.IdCliente == idCliente);
        if (!incluirInativos) query = query.Where(c => c.Ativo);
        var itens = await query.OrderBy(c => c.RazaoSocial).ToListAsync();
        return Ok(itens.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteFinalResponse>> ObterPorId(Guid id)
    {
        var item = await _db.ClientesFinais.FindAsync(id);
        if (item is null) return NotFound();
        return Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteFinalResponse>> Criar(ClienteFinalCreateRequest request)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == request.IdCliente && c.Ativo);
        if (!clienteExiste) return BadRequest("Cliente informado não existe ou está inativo.");

        var clienteFinal = new ClienteFinal
        {
            Id = Guid.NewGuid(),
            IdCliente = request.IdCliente,
            RazaoSocial = request.RazaoSocial,
            TipoInscricao = request.TipoInscricao,
            NumeroInscricao = request.NumeroInscricao,
            Email = request.Email,
            Telefone = request.Telefone,
            Ativo = true
        };

        _db.ClientesFinais.Add(clienteFinal);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = clienteFinal.Id }, ToResponse(clienteFinal));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteFinalResponse>> Atualizar(Guid id, ClienteFinalUpdateRequest request)
    {
        var item = await _db.ClientesFinais.FindAsync(id);
        if (item is null) return NotFound();

        item.RazaoSocial = request.RazaoSocial;
        item.TipoInscricao = request.TipoInscricao;
        item.NumeroInscricao = request.NumeroInscricao;
        item.Email = request.Email;
        item.Telefone = request.Telefone;
        item.Ativo = request.Ativo;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var item = await _db.ClientesFinais.FindAsync(id);
        if (item is null) return NotFound();

        item.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
