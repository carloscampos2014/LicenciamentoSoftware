using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Todas as operações de refresh token usam DbConnectionFactory diretamente —
/// não precisam de transação compartilhada com outros repositórios.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbConnectionFactory _factory;

    public RefreshTokenRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task SalvarAsync(
        Guid idUsuario, string tokenHash, DateTime expiracao,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO refresh_token (id, id_usuario, token_hash, expiracao, revogado, criado_em)
            VALUES (@Id, @IdUsuario, @TokenHash, @Expiracao, FALSE, NOW())
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    Id = Guid.NewGuid(),
                    IdUsuario = idUsuario,
                    TokenHash = tokenHash,
                    Expiracao = expiracao,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<RefreshTokenInfo?> BuscarPorHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id,
                   id_usuario   AS "IdUsuario",
                   token_hash   AS "TokenHash",
                   expiracao    AS "Expiracao",
                   revogado     AS "Revogado"
            FROM refresh_token
            WHERE token_hash = @TokenHash
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<RefreshTokenInfo>(
            new CommandDefinition(sql,
                new { TokenHash = tokenHash },
                cancellationToken: cancellationToken));
    }

    public async Task RevogarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE refresh_token SET revogado = TRUE WHERE id = @Id";

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task RevogarTodosDoUsuarioAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_token SET revogado = TRUE
            WHERE id_usuario = @IdUsuario AND revogado = FALSE
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { IdUsuario = idUsuario },
                cancellationToken: cancellationToken));
    }
}
