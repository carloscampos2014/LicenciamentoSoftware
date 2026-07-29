using DbUp;
using DbUp.Engine;

namespace LicenciamentoSoftware.Infrastructure.Persistence;

/// <summary>
/// Aplica migrations SQL versionadas usando DbUp.
/// Os scripts ficam em Persistence/Migrations/ e são embarcados no assembly como recursos.
/// Ordem de execução: alfabética pelo nome do arquivo (V001_, V002_, ...).
/// </summary>
public sealed class DatabaseMigrator
{
    private readonly string _connectionString;

    public DatabaseMigrator(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string não pode ser vazia.", nameof(connectionString));

        _connectionString = connectionString;
    }

    /// <summary>
    /// Executa todas as migrations pendentes.
    /// Idempotente — scripts já aplicados são ignorados (tabela schemaversions).
    /// </summary>
    /// <exception cref="MigrationException">Lançada se algum script falhar.</exception>
    public void MigrateUp()
    {
        EnsureDatabase.For.PostgresqlDatabase(_connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new MigrationException(
                $"Falha ao aplicar migration '{result.ErrorScript?.Name}': {result.Error?.Message}",
                result.Error);
    }

    /// <summary>
    /// Retorna true se há scripts pendentes a aplicar.
    /// </summary>
    public bool HasPendingMigrations()
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .Build();

        return upgrader.IsUpgradeRequired();
    }
}
