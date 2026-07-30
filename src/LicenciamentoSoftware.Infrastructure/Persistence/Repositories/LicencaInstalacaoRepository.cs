using Dapper;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class LicencaInstalacaoRepository : ILicencaInstalacaoRepository
{
    private readonly DbConnectionFactory _factory;

    public LicencaInstalacaoRepository(DbConnectionFactory factory) => _factory = factory;

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

    public async Task LiberarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_instalacao_registrada SET ativo = FALSE WHERE id = @Id
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
