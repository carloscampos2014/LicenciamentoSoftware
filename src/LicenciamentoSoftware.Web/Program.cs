using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using LicenciamentoSoftware.Web;
using LicenciamentoSoftware.Web.Services;
using LicenciamentoSoftware.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = builder.HostEnvironment.BaseAddress;

// HttpClient padrão (anônimo — para BFF endpoints públicos: /bff/login, /auth/cadastrar)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(baseAddress)
});

// Autenticação baseada em JWT em memória
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthStateProvider>());

// TokenRefreshHandler como Scoped para compartilhar a mesma instância
// de JwtAuthStateProvider que tem o token em memória
builder.Services.AddScoped<TokenRefreshHandler>();
builder.Services.AddAuthorizationCore();

// HttpClient autenticado (com TokenRefreshHandler) para chamadas aos services da API
builder.Services.AddHttpClient<ClienteFinalApiService>(client =>
    client.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<TokenRefreshHandler>());

builder.Services.AddHttpClient<UsuarioApiService>(client =>
    client.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<TokenRefreshHandler>());

builder.Services.AddHttpClient<AplicacaoApiService>(client =>
    client.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<TokenRefreshHandler>());

builder.Services.AddHttpClient<TipoLicencaApiService>(client =>
    client.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<TokenRefreshHandler>());

builder.Services.AddHttpClient<LicencaApiService>(client =>
    client.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<TokenRefreshHandler>());

await builder.Build().RunAsync();
