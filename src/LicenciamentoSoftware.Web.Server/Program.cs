using LicenciamentoSoftware.Client.Extensions;
using LicenciamentoSoftware.Web.Server.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Cookie policy para o BFF — HttpOnly, Secure, SameSite=Strict
builder.Services.AddBffCookiePolicy();

// Serviços HTTP do Client (proxy para a API)
builder.Services.AddApiClientServices();
builder.Services.ConfigureApiHttpClients(builder.Configuration);

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

// Fallback para o index.html do Blazor WASM (SPA routing)
app.MapFallbackToFile("index.html");

app.Run();
