using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Controllers;

// TipoLicenca é uma tabela fixa/global (seed) - somente leitura.
[ApiController]
[Route("api/tipos-licenca")]
public class TiposLicencaController : ControllerBase
{
    private readonly LicenciamentoDbContext _db;
    public TiposLicencaController(LicenciamentoDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoLicencaResponse>>> Listar()
    {
        var itens = await _db.TiposLicenca
            .Select(t => new TipoLicencaResponse(t.Id, t.Descricao))
            .ToListAsync();
        return Ok(itens);
    }
}
