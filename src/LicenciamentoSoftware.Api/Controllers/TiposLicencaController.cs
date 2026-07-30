using LicenciamentoSoftware.Application.TipoLicenca.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// Tipos de licença disponíveis (tabela fixa — somente leitura).
/// </summary>
[ApiController]
[Route("tipos-licenca")]
[Authorize(Policy = "Leitor")]
public sealed class TiposLicencaController : ControllerBase
{
    private readonly ListarTiposLicencaHandler _listarHandler;
    private readonly BuscarTipoLicencaPorIdHandler _buscarHandler;

    public TiposLicencaController(
        ListarTiposLicencaHandler listarHandler,
        BuscarTipoLicencaPorIdHandler buscarHandler)
    {
        _listarHandler = listarHandler;
        _buscarHandler = buscarHandler;
    }

    /// <summary>Lista todos os tipos de licença disponíveis.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var resultado = await _listarHandler.HandleAsync(ct);
        return Ok(resultado);
    }

    /// <summary>Busca um tipo de licença pelo ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> BuscarPorId(Guid id, CancellationToken ct)
    {
        var resultado = await _buscarHandler.HandleAsync(id, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}
