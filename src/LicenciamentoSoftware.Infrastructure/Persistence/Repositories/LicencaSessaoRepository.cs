using Dapper;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class LicencaSessaoRepository : ILicencaSessaoRepository
{
    private readonly DbConnectionFactory _factory;

    public LicencaSessaoRepository(DbConnectionFactory factory) => _factory = factory;

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

    public async Task EncerrarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE licenca_sessao SET ativo = FALSE WHERE id = @Id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
