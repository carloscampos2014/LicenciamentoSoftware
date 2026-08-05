using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace LicenciamentoSoftware.Admin;

/// <summary>
/// Handler de HTTP Basic Authentication para o painel Admin.
/// Credenciais configuradas via Admin:Usuario e Admin:Senha (appsettings ou env).
/// </summary>
public sealed class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _config;

    public BasicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.Fail("Authorization header ausente."));

        try
        {
            var header = AuthenticationHeaderValue.Parse(authHeader!);
            if (!string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(AuthenticateResult.Fail("Scheme inválido."));

            var credenciais = Encoding.UTF8.GetString(
                Convert.FromBase64String(header.Parameter ?? string.Empty));

            var separador = credenciais.IndexOf(':', StringComparison.Ordinal);
            if (separador < 0)
                return Task.FromResult(AuthenticateResult.Fail("Credenciais malformadas."));

            var usuario = credenciais[..separador];
            var senha   = credenciais[(separador + 1)..];

            var usuarioEsperado = _config["Admin:Usuario"] ?? "admin";
            var senhaEsperada   = _config["Admin:Senha"]   ?? string.Empty;

            if (string.IsNullOrEmpty(senhaEsperada))
                return Task.FromResult(AuthenticateResult.Fail("Senha do Admin não configurada."));

            if (!string.Equals(usuario, usuarioEsperado, StringComparison.Ordinal)
                || !string.Equals(senha, senhaEsperada, StringComparison.Ordinal))
                return Task.FromResult(AuthenticateResult.Fail("Credenciais inválidas."));

            var claims    = new[] { new Claim(ClaimTypes.Name, usuario) };
            var identity  = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket    = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Erro ao processar credenciais."));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = 401;
        Response.Headers.WWWAuthenticate = "Basic realm=\"LicenciamentoSoftware Admin\"";
        return Task.CompletedTask;
    }
}
