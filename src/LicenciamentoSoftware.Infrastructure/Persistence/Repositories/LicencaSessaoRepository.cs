using Dapper;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class LicencaSessaoRepository : ILicencaSessaoRepository
{
    private readonly DbConnectionFactory _factory;
    private readonly IUnitOfWork _uow;

    public LicencaSessaoRepository(DbConnectionFactory factory, IUnitOfWork uow)
    {
        _factory = factory;
        _uow     = uow;
    }

    // -------------------------------------------------------------------------
    // Leitura (Fase 6 — gestão manual)
    // -------------------------------------------------------------------------

    public async Task<SessaoResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_usuario   AS "IdentificadorUsuario",
                   data_login              AS "DataLogin",
                   data_ultima_atividade   AS "DataUltimaAtividade",
                   ativo                   AS "Ativo"
            FROM licenca_sessao
            WHERE id = @Id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SessaoResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SessaoResult>> ListarPorLicencaAsync(
        Guid idLicenca, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_usuario   AS "IdentificadorUsuario",
                   data_login              AS "DataLogin",
                   data_ultima_atividade   AS "DataUltimaAtividade",
                   ativo                   AS "Ativo"
            FROM licenca_sessao
            WHERE licenca_id = @IdLicenca
            ORDER BY data_login DESC
            """;
        using var conn = _factory.CreateConnection();
        var itens = await conn.QueryAsync<SessaoResult>(
            new CommandDefinition(sql, new { IdLicenca = idLicenca }, cancellationToken: ct));
        return itens.AsList();
    }

    // -------------------------------------------------------------------------
    // Escrita (Fase 6 — encerramento manual)
    // -------------------------------------------------------------------------

    public async Task EncerrarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE licenca_sessao SET ativo = FALSE WHERE id = @Id";
        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // Fase 7 — validação de login (dentro de transação serializável)
    // -------------------------------------------------------------------------

    public async Task<int> ContarUsuariosDistintosAtivosPorLicencaAsync(Guid idLicenca, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(DISTINCT identificador_usuario) FROM licenca_sessao
            WHERE licenca_id = @IdLicenca AND ativo = TRUE
            """;
        return await _uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { IdLicenca = idLicenca },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    public async Task<int> ContarAtivasPorUsuarioAsync(
        Guid idLicenca, string identificadorUsuario, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*) FROM licenca_sessao
            WHERE licenca_id            = @IdLicenca
              AND identificador_usuario = @IdentificadorUsuario
              AND ativo                 = TRUE
            """;
        return await _uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql,
                new { IdLicenca = idLicenca, IdentificadorUsuario = identificadorUsuario },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    public async Task InserirAsync(
        Domain.Entities.LicencaSessao sessao, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO licenca_sessao
                (id, licenca_id, identificador_usuario, data_login, data_ultima_atividade, ativo)
            VALUES
                (@Id, @LicencaId, @IdentificadorUsuario, @DataLogin, @DataUltimaAtividade, TRUE)
            """;
        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    Id = sessao.Id,
                    LicencaId = sessao.LicencaId,
                    IdentificadorUsuario = sessao.IdentificadorUsuario,
                    DataLogin = sessao.DataLogin,
                    DataUltimaAtividade = sessao.DataUltimaAtividade,
                },
                transaction: _uow.Transaction, cancellationToken: ct));
    }

    public async Task AtualizarAtividadeAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_sessao
            SET data_ultima_atividade = NOW()
            WHERE id = @Id
            """;
        // Heartbeat não exige transação própria — usa conexão direta (sem UoW)
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // Fase 8 — job de sessões inativas
    // -------------------------------------------------------------------------

    public async Task<int> EncerrarSessoesInativasAsync(
        DateTime limiteAtividade, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_sessao
            SET ativo = FALSE
            WHERE ativo = TRUE
              AND data_ultima_atividade < @LimiteAtividade
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteAsync(
            new CommandDefinition(sql, new { LimiteAtividade = limiteAtividade },
                cancellationToken: ct));
    }
}
