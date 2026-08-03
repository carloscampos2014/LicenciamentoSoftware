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

// HttpClient padrão anônimo (BFF endpoints: /bff/login, /bff/cadastrar)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(baseAddress)
});

// HttpClient nomeado para chamadas BFF internas (ex: /bff/refresh na restauração de sessão)
builder.Services.AddHttpClient("bff", client =>
{
    client.BaseAddress = new Uri(baseAddress);
});

// ApiHttpClientFactory — singleton que mantém HttpClients com token atualizado
// Usar Singleton garante que a mesma instância (com o mesmo token) seja usada
// em toda a vida do app no browser
builder.Services.AddSingleton(new ApiHttpClientFactory(baseAddress));

// Expõe os services individualmente para injeção via @inject nas páginas
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ApiHttpClientFactory>().ClienteFinal);
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ApiHttpClientFactory>().Usuario);
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ApiHttpClientFactory>().Aplicacao);
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ApiHttpClientFactory>().TipoLicenca);
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ApiHttpClientFactory>().Licenca);
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ApiHttpClientFactory>().Dashboard);

// Autenticação — JwtAuthStateProvider como Singleton para ter acesso
// à mesma ApiHttpClientFactory e atualizar os tokens
builder.Services.AddSingleton<JwtAuthStateProvider>(sp =>
{
    var provider = new JwtAuthStateProvider(sp.GetRequiredService<IHttpClientFactory>());
    var factory = sp.GetRequiredService<ApiHttpClientFactory>();
    provider.SetApiFactory(factory);
    return provider;
});
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
