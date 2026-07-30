using Dapper;
using LicenciamentoSoftware.Application.TipoLicenca.Abstractions;
using LicenciamentoSoftware.Application.TipoLicenca.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class TipoLicencaRepository : ITipoLicencaRepository
{
    private readonly DbConnectionFactory _factory;

    public TipoLicencaRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<TipoLicencaResult>> ListarAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT id        AS "Id",
                   descricao AS "Descricao"
            FROM tipo_licenca
            ORDER BY descricao
            """;
        using var conn = _factory.CreateConnection();
        var itens = await conn.QueryAsync<TipoLicencaResult>(
            new CommandDefinition(sql, cancellationToken: ct));
        return itens.AsList();
    }

    public async Task<TipoLicencaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id        AS "Id",
                   descricao AS "Descricao"
            FROM tipo_licenca
            WHERE id = @Id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<TipoLicencaResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
