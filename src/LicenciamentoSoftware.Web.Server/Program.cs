using LicenciamentoSoftware.Client.Extensions;
using LicenciamentoSoftware.Web.Server.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Cookie policy para o BFF — HttpOnly, Secure, SameSite=Strict
builder.Services.AddBffCookiePolicy();

// Serviços HTTP do Client (proxy para a API — usado pelo BffController)
builder.Services.AddApiClientServices();
builder.Services.ConfigureApiHttpClients(builder.Configuration);

// YARP — proxy reverso transparente para todos os endpoints da API
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(ctx =>
        {
            if (ctx.HttpContext.Request.Headers.TryGetValue("Authorization", out var auth)
                && !string.IsNullOrEmpty(auth))
            {
                ctx.ProxyRequest.Headers.Remove("Authorization");
                ctx.ProxyRequest.Headers.TryAddWithoutValidation("Authorization", (string?)auth);
            }
            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();

// Arquivos estáticos — CSS/JS com versionamento não deve ser cacheado agressivamente
// pelo CDN para evitar que versões antigas sejam servidas após deploy.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        // Arquivos com hash no nome (Blazor framework files) podem cachear por muito tempo.
        // Arquivos sem hash (app.css, app.js) não devem ser cacheados pelo CDN.
        if (path.Equals("app.css", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("app.js", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers.Pragma = "no-cache";
            ctx.Context.Response.Headers.Expires = "0";
        }
    }
});

app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// YARP — repassa chamadas da API para localhost:7075
// O header Authorization (Bearer token) é propagado automaticamente
app.MapReverseProxy();

// Fallback para o index.html do Blazor WASM (SPA routing)
// Cobre qualquer rota não reconhecida — permite F5/reload em qualquer página
app.MapFallbackToFile("index.html");

app.Run();
