using Dapper;
using FluentAssertions;
using LicenciamentoSoftware.Infrastructure.Persistence;
using Npgsql;

namespace LicenciamentoSoftware.IntegrationTests.Persistence;

/// <summary>
/// Valida que a migration V001 aplica corretamente em banco limpo.
///
/// Pré-requisito: PostgreSQL acessível via variável de ambiente
///   ConnectionStrings__DefaultConnection
///   Ex: "Host=localhost;Port=5432;Database=postgres;Username=licenciamento;Password=SUA_SENHA"
///
/// Execução local (WSL2):
///   $env:ConnectionStrings__DefaultConnection = "Host=localhost;..."
///   dotnet test --filter "FullyQualifiedName~MigrationTests"
/// </summary>
[Trait("Category", "Integration")]
public class MigrationTests : IDisposable
{
    private readonly string _connectionString;
    private readonly string _testDatabase;
    private bool _disposed;

    public MigrationTests()
    {
        var baseConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "Variável ConnectionStrings__DefaultConnection não definida. " +
                "Configure antes de rodar testes de integração.");

        _testDatabase = $"licenciamento_migration_{Guid.NewGuid():N}";
        _connectionString = ReplaceDatabase(baseConn, _testDatabase);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);

        try
        {
            var adminConn = ReplaceDatabase(_connectionString, "postgres");
            using var conn = new NpgsqlConnection(adminConn);
            conn.Open();
            conn.Execute($"DROP DATABASE IF EXISTS \"{_testDatabase}\" WITH (FORCE)");
        }
        catch
        {
            // Ignora falha no cleanup
        }
    }

    [Fact]
    public void MigrateUp_BancoLimpo_AplicaSchemaComSucesso()
    {
        var migrator = new DatabaseMigrator(_connectionString);
        var act = () => migrator.MigrateUp();
        act.Should().NotThrow();
    }

    [Fact]
    public void MigrateUp_AplicadaDuasVezes_EIdempotente()
    {
        var migrator = new DatabaseMigrator(_connectionString);
        migrator.MigrateUp();

        var act = () => migrator.MigrateUp();
        act.Should().NotThrow();
    }

    [Fact]
    public void MigrateUp_TabelasEsperadas_ExistemNoBanco()
    {
        var migrator = new DatabaseMigrator(_connectionString);
        migrator.MigrateUp();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        var tabelas = conn.Query<string>(
            @"SELECT table_name
              FROM information_schema.tables
              WHERE table_schema = 'public'
                AND table_type   = 'BASE TABLE'").ToList();

        tabelas.Should().Contain([
            "cliente", "usuario", "cliente_final", "tipo_licenca",
            "aplicacao", "licenca", "licenca_periodo", "licenca_usuarios",
            "licenca_sessao", "licenca_instalacao",
            "licenca_instalacao_registrada", "log_operacao"
        ]);
    }

    [Fact]
    public void MigrateUp_SeedTipoLicenca_Inseriu4Registros()
    {
        var migrator = new DatabaseMigrator(_connectionString);
        migrator.MigrateUp();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        var descricoes = conn.Query<string>(
            "SELECT descricao FROM tipo_licenca ORDER BY descricao").ToList();

        descricoes.Should().HaveCount(4);
        descricoes.Should().BeEquivalentTo([
            "Permanente", "Por Período", "Por Usuários", "Por Instalação"
        ]);
    }

    [Fact]
    public void MigrateUp_ConstraintUnicaLicenca_Existe()
    {
        var migrator = new DatabaseMigrator(_connectionString);
        migrator.MigrateUp();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        var indice = conn.QueryFirstOrDefault<string>(
            @"SELECT indexname FROM pg_indexes
              WHERE tablename = 'licenca'
                AND indexname = 'uq_licenca_combinacao_ativa'");

        indice.Should().Be("uq_licenca_combinacao_ativa");
    }

    private static string ReplaceDatabase(string connectionString, string newDatabase)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = newDatabase };
        return builder.ConnectionString;
    }
}
