using Dapper;
using LicenciamentoSoftware.Infrastructure.Persistence;
using Npgsql;

namespace LicenciamentoSoftware.IntegrationTests.Auth;

/// <summary>
/// Banco de dados PostgreSQL isolado para os testes de integração de Auth.
/// Criado e migrado uma vez (xUnit Collection Fixture), descartado ao final.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public string ConnectionString { get; }

    private readonly string _adminConnectionString;
    private readonly string _dbName;
    private bool _disposed;

    public TestDatabase()
    {
        var baseConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "Variável ConnectionStrings__DefaultConnection não definida.");

        _dbName = $"licenciamento_auth_test_{Guid.NewGuid():N}";
        _adminConnectionString = ReplaceDatabase(baseConn, "postgres");
        ConnectionString = ReplaceDatabase(baseConn, _dbName);

        // Aplica migration no banco isolado
        var migrator = new DatabaseMigrator(ConnectionString);
        migrator.MigrateUp();
    }

    /// <summary>Limpa todas as tabelas de dados (mantém seed e schema).</summary>
    public void LimparDados()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        conn.Execute("""
            TRUNCATE TABLE refresh_token, usuario_papel, usuario,
                         log_operacao, licenca_instalacao_registrada,
                         licenca_instalacao, licenca_sessao,
                         licenca_usuarios, licenca_periodo, licenca,
                         aplicacao, cliente_final, cliente
            RESTART IDENTITY CASCADE
            """);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            using var conn = new NpgsqlConnection(_adminConnectionString);
            conn.Open();
            conn.Execute($"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)");
        }
        catch { /* ignora falha no cleanup */ }
    }

    private static string ReplaceDatabase(string connectionString, string dbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = dbName };
        return builder.ConnectionString;
    }
}

/// <summary>Collection fixture — compartilha o banco entre todos os testes de Auth.</summary>
[CollectionDefinition("AuthTests")]
public sealed class AuthTestsFixture : ICollectionFixture<TestDatabase> { }
