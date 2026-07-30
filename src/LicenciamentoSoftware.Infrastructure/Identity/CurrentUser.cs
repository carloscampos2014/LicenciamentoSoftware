using LicenciamentoSoftware.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LicenciamentoSoftware.Infrastructure.Identity;

/// <summary>
/// Lê o usuário autenticado a partir das claims do JWT injetado pelo middleware.
/// O IdCliente (tenant) vem exclusivamente do token — nunca do body da requisição.
/// Registrado como Scoped no DI.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    public Guid Id { get; }
    public Guid IdCliente { get; }
    public string Nome { get; }
    public string Papel { get; }
    public bool EstaAutenticado { get; }

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            EstaAutenticado = false;
            Nome = string.Empty;
            Papel = string.Empty;
            return;
        }

        EstaAutenticado = true;

        Id = Guid.TryParse(
            user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id : Guid.Empty;

        IdCliente = Guid.TryParse(
            user.FindFirst("id_cliente")?.Value, out var idCliente)
            ? idCliente : Guid.Empty;

        Nome = user.FindFirst("nome")?.Value ?? string.Empty;
        Papel = user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
