using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class RecuperacaoSenhaRepository : IRecuperacaoSenhaRepository
{
    private readonly IUnitOfWork _uow;
    private readonly DbConnectionFactory _factory;

    public RecuperacaoSenhaRepository(IUnitOfWork uow, DbConnectionFactory factory)
    {
        _uow     = uow;
        _factory = factory;
    }

    public async Task SalvarAsync(
        Guid idUsuario, string tokenHash, DateTime expiraEm,
        CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO senha_redefinicao (id_usuario, token_hash, expira_em)
            VALUES (@IdUsuario, @TokenHash, @ExpiraEm)
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { IdUsuario = idUsuario, TokenHash = tokenHash, ExpiraEm = expiraEm },
                transaction: _uow.Transaction,
                cancellationToken: ct));
    }

    public async Task<TokenRecuperacao?> BuscarPorHashAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id        AS "Id",
                   id_usuario AS "IdUsuario",
                   expira_em  AS "ExpiraEm"
            FROM senha_redefinicao
            WHERE token_hash = @TokenHash
              AND usado_em IS NULL
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<TokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash },
                cancellationToken: ct));

        return row is null ? null : new TokenRecuperacao(row.Id, row.IdUsuario, row.ExpiraEm);
    }

    public async Task MarcarComoUsadoAsync(Guid idToken, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE senha_redefinicao
               SET usado_em = NOW()
             WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = idToken },
                transaction: _uow.Transaction,
                cancellationToken: ct));
    }

    private sealed record TokenRow(Guid Id, Guid IdUsuario, DateTime ExpiraEm);
}
