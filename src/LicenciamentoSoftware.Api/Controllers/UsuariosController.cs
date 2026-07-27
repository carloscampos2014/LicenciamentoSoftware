using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly LicenciamentoDbContext _db;
    public UsuariosController(LicenciamentoDbContext db) => _db = db;

    private static UsuarioResponse ToResponse(Usuario u) => new(u.Id, u.IdCliente, u.Nome, u.Ativo);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar([FromQuery] Guid? idCliente, [FromQuery] bool incluirInativos = false)
    {
        var query = _db.Usuarios.AsQueryable();
        if (idCliente.HasValue) query = query.Where(u => u.IdCliente == idCliente);
        if (!incluirInativos) query = query.Where(u => u.Ativo);
        var itens = await query.OrderBy(u => u.Nome).ToListAsync();
        return Ok(itens.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioResponse>> ObterPorId(Guid id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        return Ok(ToResponse(usuario));
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Criar(UsuarioCreateRequest request)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == request.IdCliente && c.Ativo);
        if (!clienteExiste) return BadRequest("Cliente informado não existe ou está inativo.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            IdCliente = request.IdCliente,
            Nome = request.Nome,
            Ativo = true
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, ToResponse(usuario));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioResponse>> Atualizar(Guid id, UsuarioUpdateRequest request)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        usuario.Nome = request.Nome;
        usuario.Ativo = request.Ativo;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(usuario));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        usuario.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
