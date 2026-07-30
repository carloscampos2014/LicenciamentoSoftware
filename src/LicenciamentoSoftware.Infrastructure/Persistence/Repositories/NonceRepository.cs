using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de nonces anti-replay.
/// Persiste em PostgreSQL para garantir consistência em múltiplas instâncias.
/// Registros expirados são ignorados na verificação — limpeza via job externo ou TTL.
/// </summary>
public sealed class NonceRepository : INonceRepository
{
    private readonly DbConnectionFactory _factory;

    public NonceRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <inheritdoc/>
    public async Task<bool> ExisteAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        // Considera apenas nonces ainda dentro do período de validade
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM nonce_replay
                 WHERE nonce    = @Nonce
                   AND expira_em > NOW()
            )
            """;

        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql,
                new { Nonce = nonce },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc/>
    public async Task RegistrarAsync(
        string nonce,
        DateTime expiraEm,
        CancellationToken cancellationToken = default)
    {
        // ON CONFLICT DO NOTHING — idempotente em caso de retry do cliente
        const string sql = """
            INSERT INTO nonce_replay (nonce, expira_em)
            VALUES (@Nonce, @ExpiraEm)
            ON CONFLICT (nonce) DO NOTHING
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new { Nonce = nonce, ExpiraEm = expiraEm },
                cancellationToken: cancellationToken));
    }
}
