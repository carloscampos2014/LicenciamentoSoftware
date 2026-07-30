using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenciamentoSoftware.Api.Controllers;

/// <summary>
/// Endpoints de gestão de tokens HMAC por licença.
/// </summary>
[ApiController]
[Route("licencas")]
[Authorize(Policy = "OperadorCliente")]
public sealed class LicencaController : ControllerBase
{
    private readonly EmitirTokenLicencaHandler _emitirHandler;
    private readonly RenovarTokenLicencaHandler _renovarHandler;

    public LicencaController(
        EmitirTokenLicencaHandler emitirHandler,
        RenovarTokenLicencaHandler renovarHandler)
    {
        _emitirHandler = emitirHandler;
        _renovarHandler = renovarHandler;
    }

    /// <summary>
    /// Emite um token HMAC-SHA256 para a licença informada.
    /// O segredo é retornado em texto puro UMA ÚNICA VEZ — guarde-o com segurança.
    /// </summary>
    [HttpPost("{id:guid}/token")]
    public async Task<IActionResult> EmitirToken(
        Guid id,
        [FromBody] EmitirTokenRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _emitirHandler.HandleAsync(
            new EmitirTokenLicencaCommand(id, request.ExpiracaoMinutos),
            cancellationToken);

        return resultado switch
        {
            EmitirTokenResult.Sucesso s => CreatedAtAction(
                nameof(EmitirToken),
                new { id = s.IdToken },
                new
                {
                    s.IdToken,
                    s.IdLicenca,
                    s.TokenTexto,
                    s.ExpiracaoMinutos,
                    Aviso = "Este é o único momento em que o token é exibido. Guarde-o com segurança.",
                }),
            EmitirTokenResult.LicencaNaoEncontrada => NotFound(new { Erro = "Licença não encontrada." }),
            EmitirTokenResult.LicencaInativa => UnprocessableEntity(new { Erro = "Licença está inativa." }),
            EmitirTokenResult.TokenJaExiste => Conflict(new { Erro = "Já existe um token ativo para esta licença. Use o endpoint de renovação." }),
            _ => StatusCode(500),
        };
    }

    /// <summary>
    /// Renova o token HMAC da licença, revogando o anterior.
    /// O novo segredo é retornado em texto puro UMA ÚNICA VEZ.
    /// </summary>
    [HttpPost("{id:guid}/token/renovar")]
    [HttpPost("~/auth/licenca/renovar-token")]  // rota alternativa conforme issue #12
    public async Task<IActionResult> RenovarToken(
        Guid id,
        [FromBody] RenovarTokenRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _renovarHandler.HandleAsync(
            new RenovarTokenLicencaCommand(id, request.ExpiracaoMinutos),
            cancellationToken);

        return resultado switch
        {
            EmitirTokenResult.Sucesso s => Ok(new
            {
                s.IdToken,
                s.IdLicenca,
                s.TokenTexto,
                s.ExpiracaoMinutos,
                Aviso = "Este é o único momento em que o token é exibido. Guarde-o com segurança.",
            }),
            EmitirTokenResult.LicencaNaoEncontrada => NotFound(new { Erro = "Licença não encontrada." }),
            EmitirTokenResult.LicencaInativa => UnprocessableEntity(new { Erro = "Licença está inativa." }),
            _ => StatusCode(500),
        };
    }
}

// ----- Request DTOs -----

public sealed record EmitirTokenRequest(int? ExpiracaoMinutos = null);
public sealed record RenovarTokenRequest(int? ExpiracaoMinutos = null);
