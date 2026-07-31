using LicenciamentoSoftware.Client.Extensions;
using LicenciamentoSoftware.Web.Server.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Cookie policy para o BFF — HttpOnly, Secure, SameSite=Strict
builder.Services.AddBffCookiePolicy();

// Serviços HTTP do Client (proxy para a API — usado pelo BffController)
builder.Services.AddApiClientServices();
builder.Services.ConfigureApiHttpClients(builder.Configuration);

// YARP — proxy reverso transparente para todos os endpoints da API
// O WASM chama /clientes-finais, /licencas, etc. no Web.Server,
// que repassa para a API com o Bearer token no header
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
app.UseStaticFiles();

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
app.MapFallbackToFile("index.html");

app.Run();
