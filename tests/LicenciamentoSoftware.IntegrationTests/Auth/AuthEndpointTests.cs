using Dapper;
using FluentAssertions;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LicenciamentoSoftware.IntegrationTests.Auth;

[Trait("Category", "Integration")]
[Collection("AuthTests")]
public sealed class AuthEndpointTests : IDisposable
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly TestDatabase _db;

    public AuthEndpointTests(TestDatabase db)
    {
        _db = db;
        _db.LimparDados();
        _factory = new ApiWebApplicationFactory(db.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<Guid> CriarClienteNoBancoAsync()
    {
        var idCliente = Guid.NewGuid();
        using var conn = new NpgsqlConnection(_db.ConnectionString);
        conn.Open();
        await conn.ExecuteAsync("""
            INSERT INTO cliente (id, razao_social, tipo_inscricao, numero_inscricao, email, ativo)
            VALUES (@Id, 'Empresa Teste', 2, '11222333000181', 'empresa@teste.com', true)
            """, new { Id = idCliente });
        return idCliente;
    }

    [Fact]
    public async Task Register_DadosValidos_RetornaCreated()
    {
        var idCliente = await CriarClienteNoBancoAsync();

        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            idCliente,
            nome = "Admin Teste",
            email = "admin@teste.com",
            senha = "Senha@123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("papel").GetString().Should().Be("AdministradorCliente");
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_RetornaUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "inexistente@teste.com",
            senha = "senhaqualquer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_SemTotp_RetornaTokens()
    {
        var idCliente = await CriarClienteNoBancoAsync();

        // Registra usuário
        await _client.PostAsJsonAsync("/auth/register", new
        {
            idCliente,
            nome = "Usuário Login",
            email = "login@teste.com",
            senha = "Senha@123456"
        });

        // Faz login
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "login@teste.com",
            senha = "Senha@123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_Retorna401()
    {
        // /auth/logout requer Authorize
        var response = await _client.PostAsJsonAsync("/auth/logout", new
        {
            refreshToken = "qualquer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_TokenValido_RetornaNovosPar()
    {
        var idCliente = await CriarClienteNoBancoAsync();

        await _client.PostAsJsonAsync("/auth/register", new
        {
            idCliente,
            nome = "Refresh Teste",
            email = "refresh@teste.com",
            senha = "Senha@123456"
        });

        var loginResp = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "refresh@teste.com",
            senha = "Senha@123456"
        });

        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        var refreshResp = await _client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken
        });

        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResp.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_TokenRevogado_Retorna401()
    {
        var idCliente = await CriarClienteNoBancoAsync();

        await _client.PostAsJsonAsync("/auth/register", new
        {
            idCliente,
            nome = "Revogado Teste",
            email = "revogado@teste.com",
            senha = "Senha@123456"
        });

        var loginResp = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "revogado@teste.com",
            senha = "Senha@123456"
        });

        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;
        var accessToken = loginBody.GetProperty("accessToken").GetString()!;

        // Faz logout (revoga o refresh token)
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        await _client.PostAsJsonAsync("/auth/logout", new { refreshToken });
        _client.DefaultRequestHeaders.Authorization = null;

        // Tenta usar o refresh token revogado
        var refreshResp = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
