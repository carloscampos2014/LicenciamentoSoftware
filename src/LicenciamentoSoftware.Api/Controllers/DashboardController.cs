using LicenciamentoSoftware.Application.Dashboard.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// Endpoints de métricas do dashboard — retornam dados do tenant do usuário autenticado.
/// IdCliente é sempre lido do JWT via ICurrentUser — nunca do body ou query string.
/// </summary>
[ApiController]
[Route("dashboard")]
[Authorize]
public sealed class DashboardController(
    BuscarDashboardResumoHandler resumoHandler,
    BuscarDashboardAlertasHandler alertasHandler) : ControllerBase
{
    /// <summary>
    /// Retorna as métricas gerais do tenant: totais de clientes finais,
    /// aplicações, licenças por tipo, sessões ativas, tokens expirando e novos cadastros.
    /// Executa em uma única query SQL com CTEs.
    /// </summary>
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo(CancellationToken ct)
    {
        var resultado = await resumoHandler.HandleAsync(ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Retorna alertas operacionais: sessões inativas, instalações adormecidas,
    /// licenças no limite de capacidade e erros de validação nas últimas 24h.
    /// </summary>
    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas(CancellationToken ct)
    {
        var resultado = await alertasHandler.HandleAsync(ct);
        return Ok(resultado);
    }
}
