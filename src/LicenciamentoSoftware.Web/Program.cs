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

// HttpClient padrão (anônimo — para BFF endpoints: /bff/login, /bff/cadastrar)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(baseAddress)
});

// Autenticação baseada em JWT em memória
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// Handlers registrados como Scoped para compartilhar a instância
// do JwtAuthStateProvider que contém o token em memória
builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddScoped<TokenRefreshHandler>();

// HttpClients autenticados — BearerTokenHandler adiciona o token,
// TokenRefreshHandler renova silenciosamente quando recebe 401
builder.Services.AddHttpClient<ClienteFinalApiService>(c =>
    c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddHttpMessageHandler<TokenRefreshHandler>();

builder.Services.AddHttpClient<UsuarioApiService>(c =>
    c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddHttpMessageHandler<TokenRefreshHandler>();

builder.Services.AddHttpClient<AplicacaoApiService>(c =>
    c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddHttpMessageHandler<TokenRefreshHandler>();

builder.Services.AddHttpClient<TipoLicencaApiService>(c =>
    c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddHttpMessageHandler<TokenRefreshHandler>();

builder.Services.AddHttpClient<LicencaApiService>(c =>
    c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>()
    .AddHttpMessageHandler<TokenRefreshHandler>();

await builder.Build().RunAsync();
