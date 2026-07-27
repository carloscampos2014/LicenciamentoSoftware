using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("api/aplicacoes")]
public class AplicacoesController : ControllerBase
{
    private readonly LicenciamentoDbContext _db;
    public AplicacoesController(LicenciamentoDbContext db) => _db = db;

    private static AplicacaoResponse ToResponse(Aplicacao a) =>
        new(a.Id, a.IdCliente, a.Titulo, a.Descricao, a.IdTipoLicenca, a.Ativo);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AplicacaoResponse>>> Listar([FromQuery] Guid? idCliente, [FromQuery] bool incluirInativos = false)
    {
        var query = _db.Aplicacoes.AsQueryable();
        if (idCliente.HasValue) query = query.Where(a => a.IdCliente == idCliente);
        if (!incluirInativos) query = query.Where(a => a.Ativo);
        var itens = await query.OrderBy(a => a.Titulo).ToListAsync();
        return Ok(itens.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AplicacaoResponse>> ObterPorId(Guid id)
    {
        var item = await _db.Aplicacoes.FindAsync(id);
        if (item is null) return NotFound();
        return Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<AplicacaoResponse>> Criar(AplicacaoCreateRequest request)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == request.IdCliente && c.Ativo);
        if (!clienteExiste) return BadRequest("Cliente informado não existe ou está inativo.");

        var tipoLicencaExiste = await _db.TiposLicenca.AnyAsync(t => t.Id == request.IdTipoLicenca);
        if (!tipoLicencaExiste) return BadRequest("Tipo de licença informado não existe.");

        var aplicacao = new Aplicacao
        {
            Id = Guid.NewGuid(),
            IdCliente = request.IdCliente,
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            IdTipoLicenca = request.IdTipoLicenca,
            Ativo = true
        };

        _db.Aplicacoes.Add(aplicacao);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = aplicacao.Id }, ToResponse(aplicacao));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AplicacaoResponse>> Atualizar(Guid id, AplicacaoUpdateRequest request)
    {
        var item = await _db.Aplicacoes.FindAsync(id);
        if (item is null) return NotFound();

        var tipoLicencaExiste = await _db.TiposLicenca.AnyAsync(t => t.Id == request.IdTipoLicenca);
        if (!tipoLicencaExiste) return BadRequest("Tipo de licença informado não existe.");

        item.Titulo = request.Titulo;
        item.Descricao = request.Descricao;
        item.IdTipoLicenca = request.IdTipoLicenca;
        item.Ativo = request.Ativo;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var item = await _db.Aplicacoes.FindAsync(id);
        if (item is null) return NotFound();

        item.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
