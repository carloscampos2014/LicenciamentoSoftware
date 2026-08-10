using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de tokens HMAC de licença.
/// Leituras: DbConnectionFactory (sem transação).
/// Escritas: chamadas dentro de IUnitOfWork (transação gerenciada pelo handler).
/// </summary>
public sealed class LicencaTokenRepository : ILicencaTokenRepository
{
    private readonly DbConnectionFactory _factory;

    public LicencaTokenRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task SalvarAsync(
        Guid id,
        Guid idLicenca,
        string segredoHash,
        int expiracaoMinutos,
        DateTime criadoEm,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO licenca_token (id, id_licenca, segredo_hash, expiracao_minutos, criado_em, ativo)
            VALUES (@Id, @IdLicenca, @SegredoHash, @ExpiracaoMinutos, @CriadoEm, TRUE)
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    Id = id,
                    IdLicenca = idLicenca,
                    SegredoHash = segredoHash,
                    ExpiracaoMinutos = expiracaoMinutos,
                    CriadoEm = criadoEm,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<LicencaTokenInfo?> BuscarAtivoporLicencaAsync(
        Guid idLicenca,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id                  AS "Id",
                   id_licenca          AS "IdLicenca",
                   segredo_hash        AS "SegredoHash",
                   expiracao_minutos   AS "ExpiracaoMinutos",
                   criado_em           AS "CriadoEm",
                   ativo               AS "Ativo"
            FROM licenca_token
            WHERE id_licenca = @IdLicenca
              AND ativo = TRUE
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<LicencaTokenInfo>(
            new CommandDefinition(sql,
                new { IdLicenca = idLicenca },
                cancellationToken: cancellationToken));
    }

    public async Task RevogarPorLicencaAsync(
        Guid idLicenca,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE licenca_token
               SET ativo = FALSE
             WHERE id_licenca = @IdLicenca
               AND ativo = TRUE
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new { IdLicenca = idLicenca },
                cancellationToken: cancellationToken));
    }

    public async Task AtualizarAsync(
        Guid id,
        string novoSegredoHash,
        int expiracaoMinutos,
        DateTime criadoEm,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE licenca_token
               SET segredo_hash      = @SegredoHash,
                   expiracao_minutos = @ExpiracaoMinutos,
                   criado_em         = @CriadoEm,
                   ativo             = TRUE
             WHERE id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    Id = id,
                    SegredoHash = novoSegredoHash,
                    ExpiracaoMinutos = expiracaoMinutos,
                    CriadoEm = criadoEm,
                },
                cancellationToken: cancellationToken));
    }

    // -------------------------------------------------------------------------
    // Fase 8 — jobs de rotação e notificação
    // -------------------------------------------------------------------------

    public async Task<IReadOnlyList<Application.Jobs.LicencaTokenJobInfo>> BuscarTokensProximosVencimentoAsync(
        int diasAntecedencia,
        CancellationToken cancellationToken = default)
    {
        // Um token expira em criado_em + expiracao_minutos.
        // Considera "próximo" se vencer dentro de diasAntecedencia dias a partir de agora.
        const string sql = """
            SELECT lt.id                AS "IdToken",
                   lt.id_licenca        AS "IdLicenca",
                   l.id_cliente         AS "IdCliente",
                   a.titulo             AS "NomeAplicacao",
                   lt.expiracao_minutos AS "ExpiracaoMinutos",
                   lt.criado_em         AS "CriadoEm",
                   lt.ativo             AS "Ativo"
            FROM licenca_token lt
            JOIN licenca l   ON l.id = lt.id_licenca
            JOIN aplicacao a ON a.id = l.id_aplicativo
            WHERE lt.ativo = TRUE
              AND (lt.criado_em + (lt.expiracao_minutos * INTERVAL '1 minute'))
                  <= (NOW() + (@Dias * INTERVAL '1 day'))
            ORDER BY lt.criado_em
            """;

        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<Application.Jobs.LicencaTokenJobInfo>(
            new CommandDefinition(sql, new { Dias = diasAntecedencia },
                cancellationToken: cancellationToken));
        return rows.AsList();
    }
}