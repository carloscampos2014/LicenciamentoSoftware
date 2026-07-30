using Dapper;
using FluentAssertions;
using Npgsql;
using System.Net.Http.Json;
using System.Text.Json;

namespace LicenciamentoSoftware.IntegrationTests.Auth;

/// <summary>
/// Garante que operações de escrita geram entrada no log_operacao.
/// </summary>
[Trait("Category", "Integration")]
[Collection("AuthTests")]
public sealed class AuditLogTests : IDisposable
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly TestDatabase _db;

    public AuditLogTests(TestDatabase db)
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

    private async Task<Guid> CriarClienteAsync()
    {
        var id = Guid.NewGuid();
        using var conn = new NpgsqlConnection(_db.ConnectionString);
        conn.Open();
        await conn.ExecuteAsync("""
            INSERT INTO cliente (id, razao_social, tipo_inscricao, numero_inscricao, email, ativo)
            VALUES (@Id, 'Empresa Audit', 2, '11222333000181', 'audit@empresa.com', true)
            """, new { Id = id });
        return id;
    }

    [Fact]
    public async Task Register_GeraLogDeInsercaoNoUsuario()
    {
        var idCliente = await CriarClienteAsync();

        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            idCliente,
            nome = "Audit Teste",
            email = "auditlog@teste.com",
            senha = "Senha@123456"
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var idUsuario = body.GetProperty("idUsuario").GetString()!;

        // Verifica se o log foi gerado para este usuário
        using var conn = new NpgsqlConnection(_db.ConnectionString);
        conn.Open();

        var logs = (await conn.QueryAsync<dynamic>("""
            SELECT entidade, operacao, id_registro
            FROM log_operacao
            WHERE entidade = 'Usuario'
              AND id_registro = @IdRegistro
            """, new { IdRegistro = Guid.Parse(idUsuario) })).ToList();

        logs.Should().HaveCount(1);
        ((string)logs[0].operacao).Should().Be("I");
    }
}
