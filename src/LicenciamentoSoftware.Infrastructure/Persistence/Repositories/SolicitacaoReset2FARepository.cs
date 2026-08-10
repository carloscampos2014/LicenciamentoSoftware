using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class SolicitacaoReset2FARepository : ISolicitacaoReset2FARepository
{
    private readonly IUnitOfWork _uow;
    private readonly DbConnectionFactory _factory;

    public SolicitacaoReset2FARepository(IUnitOfWork uow, DbConnectionFactory factory)
    {
        _uow     = uow;
        _factory = factory;
    }

    public async Task SalvarTokenAsync(
        Guid idUsuario, string tokenHash, DateTime expiraEm,
        string? ipOrigem, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO solicitacao_reset_2fa (id_usuario, token_hash, token_expira_em, ip_origem)
            VALUES (@IdUsuario, @TokenHash, @TokenExpiraEm, @IpOrigem)
            """;

        // Operação independente — abre sua própria conexão sem precisar de UnitOfWork
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new { IdUsuario = idUsuario, TokenHash = tokenHash,
                      TokenExpiraEm = expiraEm, IpOrigem = ipOrigem },
                cancellationToken: ct));
    }

    public async Task<TokenConfirmacaoReset?> BuscarTokenAsync(
        string tokenHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id              AS "Id",
                   id_usuario      AS "IdUsuario",
                   token_expira_em AS "ExpiraEm",
                   ip_origem       AS "IpOrigem"
            FROM solicitacao_reset_2fa
            WHERE token_hash     = @TokenHash
              AND token_usado_em IS NULL
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<TokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: ct));

        return row is null
            ? null
            : new TokenConfirmacaoReset(row.Id, row.IdUsuario, row.ExpiraEm, row.IpOrigem);
    }

    public async Task<Guid> ConfirmarECriarSolicitacaoAsync(
        Guid idToken, CancellationToken ct = default)
    {
        // Marca o token como usado e define status = Pendente (já é o default, mas deixa explícito)
        const string sql = """
            UPDATE solicitacao_reset_2fa
               SET token_usado_em = NOW(),
                   status         = 'Pendente'
             WHERE id = @Id
            RETURNING id
            """;

        var id = await _uow.Connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, new { Id = idToken },
                transaction: _uow.Transaction,
                cancellationToken: ct));

        return id;
    }

    public async Task<IReadOnlyList<SolicitacaoReset2FAInfo>> ListarPendentesAsync(
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT s.id            AS "Id",
                   s.id_usuario    AS "IdUsuario",
                   u.nome          AS "NomeUsuario",
                   u.email         AS "EmailUsuario",
                   c.razao_social  AS "NomeCliente",
                   s.status        AS "Status",
                   s.ip_origem     AS "IpOrigem",
                   s.criado_em     AS "CriadoEm"
            FROM solicitacao_reset_2fa s
            INNER JOIN usuario u ON u.id = s.id_usuario
            INNER JOIN cliente c ON c.id = u.id_cliente
            WHERE s.status = 'Pendente'
            ORDER BY s.criado_em DESC
            """;

        using var conn = _factory.CreateConnection();
        return (await conn.QueryAsync<SolicitacaoReset2FAInfo>(
            new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<SolicitacaoReset2FAInfo?> BuscarPorIdAsync(
        Guid idSolicitacao, CancellationToken ct = default)
    {
        const string sql = """
            SELECT s.id            AS "Id",
                   s.id_usuario    AS "IdUsuario",
                   u.nome          AS "NomeUsuario",
                   u.email         AS "EmailUsuario",
                   c.razao_social  AS "NomeCliente",
                   s.status        AS "Status",
                   s.ip_origem     AS "IpOrigem",
                   s.criado_em     AS "CriadoEm"
            FROM solicitacao_reset_2fa s
            INNER JOIN usuario u ON u.id = s.id_usuario
            INNER JOIN cliente c ON c.id = u.id_cliente
            WHERE s.id = @Id
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SolicitacaoReset2FAInfo>(
            new CommandDefinition(sql, new { Id = idSolicitacao }, cancellationToken: ct));
    }

    public async Task AprovarAsync(Guid idSolicitacao, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE solicitacao_reset_2fa
               SET status        = 'Aprovado',
                   processado_em = NOW()
             WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = idSolicitacao },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    public async Task RejeitarAsync(Guid idSolicitacao, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE solicitacao_reset_2fa
               SET status        = 'Rejeitado',
                   processado_em = NOW()
             WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = idSolicitacao },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    private sealed record TokenRow(Guid Id, Guid IdUsuario, DateTime ExpiraEm, string? IpOrigem);
}
