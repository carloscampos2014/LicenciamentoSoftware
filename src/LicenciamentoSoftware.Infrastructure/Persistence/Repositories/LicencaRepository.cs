using Dapper;
using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de leitura mínima de licenças — escopo da Fase 4.
/// Apenas consultas via DbConnectionFactory (sem transação).
/// </summary>
public sealed class LicencaRepository : ILicencaRepository
{
    private readonly DbConnectionFactory _factory;

    public LicencaRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<LicencaInfo?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id                   AS "Id",
                   id_cliente           AS "IdCliente",
                   id_cliente_final     AS "IdClienteFinal",
                   id_aplicativo        AS "IdAplicativo",
                   ativo                AS "Ativo"
            FROM licenca
            WHERE id = @Id
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<LicencaInfo>(
            new CommandDefinition(sql,
                new { Id = id },
                cancellationToken: cancellationToken));
    }
}
