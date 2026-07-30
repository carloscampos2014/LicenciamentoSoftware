using LicenciamentoSoftware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LicenciamentoSoftware.IntegrationTests.Auth;

/// <summary>
/// Factory que sobe a API em memória para os testes de integração.
/// Override de configuração injeta connection string e JWT secret de teste.
/// A migration é aplicada uma vez no TestDatabase antes dos testes.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["JwtSettings:Secret"] = "chave_de_teste_segura_para_integracao_32chars!",
                ["JwtSettings:Emissor"] = "LicenciamentoSoftware",
                ["JwtSettings:Audiencia"] = "LicenciamentoSoftware",
                ["JwtSettings:AccessTokenMinutos"] = "60",
            });
        });
    }
}
