using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IUnitOfWork _uow;

    public RefreshTokenRepository(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task SalvarAsync(
        Guid idUsuario, string tokenHash, DateTime expiracao,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO refresh_token (id, id_usuario, token_hash, expiracao, revogado, criado_em)
            VALUES (@Id, @IdUsuario, @TokenHash, @Expiracao, FALSE, NOW())
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    Id = Guid.NewGuid(),
                    IdUsuario = idUsuario,
                    TokenHash = tokenHash,
                    Expiracao = expiracao,
                },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<RefreshTokenInfo?> BuscarPorHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, id_usuario, token_hash, expiracao, revogado
            FROM refresh_token
            WHERE token_hash = @TokenHash
            LIMIT 1
            """;

        return await _uow.Connection.QueryFirstOrDefaultAsync<RefreshTokenInfo>(
            new CommandDefinition(sql,
                new { TokenHash = tokenHash },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task RevogarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_token SET revogado = TRUE WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task RevogarTodosDoUsuarioAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_token SET revogado = TRUE
            WHERE id_usuario = @IdUsuario AND revogado = FALSE
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { IdUsuario = idUsuario },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }
}
