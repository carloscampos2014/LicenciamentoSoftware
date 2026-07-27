using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("api")]
public class LicencaValidacaoController : ControllerBase
{
    private readonly ILicencaValidacaoService _service;

    public LicencaValidacaoController(ILicencaValidacaoService service)
    {
        _service = service;
    }

    // Licença Por Usuários
    [HttpPost("validar-login")]
    public async Task<ActionResult<ValidacaoResponse>> ValidarLogin(ValidarLoginRequest request)
    {
        var resultado = await _service.ValidarLoginAsync(request);
        return resultado.Liberado ? Ok(resultado) : StatusCode(403, resultado);
    }

    [HttpPost("heartbeat")]
    public async Task<ActionResult<ValidacaoResponse>> Heartbeat(HeartbeatRequest request)
    {
        var resultado = await _service.HeartbeatAsync(request);
        return resultado.Liberado ? Ok(resultado) : NotFound(resultado);
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ValidacaoResponse>> Logout(LogoutRequest request)
    {
        var resultado = await _service.LogoutAsync(request);
        return resultado.Liberado ? Ok(resultado) : NotFound(resultado);
    }

    // Licença Por Instalação
    [HttpPost("validar-instalacao")]
    public async Task<ActionResult<ValidacaoResponse>> ValidarInstalacao(ValidarInstalacaoRequest request)
    {
        var resultado = await _service.ValidarInstalacaoAsync(request);
        return resultado.Liberado ? Ok(resultado) : StatusCode(403, resultado);
    }
}
