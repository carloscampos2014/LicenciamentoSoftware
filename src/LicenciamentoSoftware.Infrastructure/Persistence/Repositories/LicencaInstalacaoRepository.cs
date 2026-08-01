using Dapper;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class LicencaInstalacaoRepository : ILicencaInstalacaoRepository
{
    private readonly DbConnectionFactory _factory;
    private readonly IUnitOfWork _uow;

    public LicencaInstalacaoRepository(DbConnectionFactory factory, IUnitOfWork uow)
    {
        _factory = factory;
        _uow     = uow;
    }

    // -------------------------------------------------------------------------
    // Leitura (Fase 6 — gestão manual)
    // -------------------------------------------------------------------------

    public async Task<InstalacaoRegistradaResult?> BuscarPorIdAsync(
        Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_maquina   AS "IdentificadorMaquina",
                   data_registro           AS "DataRegistro",
                   ativo                   AS "Ativo"
            FROM licenca_instalacao_registrada
            WHERE id = @Id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<InstalacaoRegistradaResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<InstalacaoRegistradaResult>> ListarPorLicencaAsync(
        Guid idLicenca, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_maquina   AS "IdentificadorMaquina",
                   data_registro           AS "DataRegistro",
                   ativo                   AS "Ativo"
            FROM licenca_instalacao_registrada
            WHERE licenca_id = @IdLicenca
            ORDER BY data_registro DESC
            """;
        using var conn = _factory.CreateConnection();
        var itens = await conn.QueryAsync<InstalacaoRegistradaResult>(
            new CommandDefinition(sql, new { IdLicenca = idLicenca }, cancellationToken: ct));
        return itens.AsList();
    }

    // -------------------------------------------------------------------------
    // Escrita (Fase 6 — liberação manual)
    // -------------------------------------------------------------------------

    public async Task LiberarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_instalacao_registrada SET ativo = FALSE WHERE id = @Id
            """;
        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // Fase 7 — validação de instalação (dentro de transação serializável)
    // -------------------------------------------------------------------------

    public async Task<InstalacaoRegistradaResult?> BuscarRegistradaAtivaAsync(
        Guid idLicenca, string identificadorMaquina, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_maquina   AS "IdentificadorMaquina",
                   data_registro           AS "DataRegistro",
                   ativo                   AS "Ativo"
            FROM licenca_instalacao_registrada
            WHERE licenca_id           = @IdLicenca
              AND identificador_maquina = @IdentificadorMaquina
              AND ativo                 = TRUE
            LIMIT 1
            """;
        return await _uow.Connection.QueryFirstOrDefaultAsync<InstalacaoRegistradaResult>(
            new CommandDefinition(sql,
                new { IdLicenca = idLicenca, IdentificadorMaquina = identificadorMaquina },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    public async Task<int> ContarAtivasAsync(Guid idLicenca, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*) FROM licenca_instalacao_registrada
            WHERE licenca_id = @IdLicenca AND ativo = TRUE
            """;
        return await _uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { IdLicenca = idLicenca },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    public async Task InserirRegistradaAsync(
        Domain.Entities.LicencaInstalacaoRegistrada instalacao, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO licenca_instalacao_registrada
                (id, licenca_id, identificador_maquina, data_registro, ativo)
            VALUES
                (@Id, @LicencaId, @IdentificadorMaquina, @DataRegistro, TRUE)
            """;
        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    Id = instalacao.Id,
                    LicencaId = instalacao.LicencaId,
                    IdentificadorMaquina = instalacao.IdentificadorMaquina,
                    DataRegistro = instalacao.DataRegistro,
                },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // Fase 9.1 — atualização de última validação (dashboard)
    // -------------------------------------------------------------------------

    public async Task AtualizarUltimaValidacaoAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_instalacao_registrada
               SET data_ultima_validacao = NOW()
             WHERE id = @Id
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
