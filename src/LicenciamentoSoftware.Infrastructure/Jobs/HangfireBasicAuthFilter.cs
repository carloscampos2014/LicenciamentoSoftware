using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace LicenciamentoSoftware.Infrastructure.Jobs;

/// <summary>
/// Filtro de autorização Basic Auth para o dashboard do Hangfire.
/// </summary>
public sealed class HangfireBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _user;
    private readonly string _password;

    public HangfireBasicAuthFilter(string user, string password)
    {
        _user     = user;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Challenge(httpContext);
            return false;
        }

        try
        {
            var encoded     = authHeader["Basic ".Length..].Trim();
            var decoded     = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts       = decoded.Split(':', 2);
            var user        = parts[0];
            var password    = parts.Length > 1 ? parts[1] : string.Empty;

            if (user == _user && password == _password)
                return true;
        }
        catch
        {
            // credenciais mal formadas — nega
        }

        Challenge(httpContext);
        return false;
    }

    private static void Challenge(HttpContext ctx)
    {
        ctx.Response.StatusCode  = 401;
        ctx.Response.Headers["WWW-Authenticate"] = "Basic realm=\"LicenseManager Jobs\"";
    }
}
