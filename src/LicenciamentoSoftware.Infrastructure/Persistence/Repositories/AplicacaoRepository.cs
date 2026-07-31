using Dapper;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class AplicacaoRepository : IAplicacaoRepository
{
    private readonly DbConnectionFactory _factory;

    public AplicacaoRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<AplicacaoResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT a.id               AS "Id",
                   a.id_cliente       AS "IdCliente",
                   a.titulo           AS "Titulo",
                   a.descricao        AS "Descricao",
                   a.id_tipo_licenca  AS "IdTipoLicenca",
                   tl.descricao       AS "TipoLicencaDescricao",
                   a.ativo            AS "Ativo"
            FROM aplicacao a
            JOIN tipo_licenca tl ON tl.id = a.id_tipo_licenca
            WHERE a.id = @Id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AplicacaoResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExisteTipoLicencaAsync(Guid idTipoLicenca, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tipo_licenca WHERE id = @Id)";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = idTipoLicenca }, cancellationToken: ct));
    }

    public async Task<PagedResult<AplicacaoResult>> ListarAsync(
        Guid? idCliente, string? titulo, bool? ativo,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        const string sqlCount = """
            SELECT COUNT(*) FROM aplicacao a
            WHERE (@IdCliente IS NULL OR a.id_cliente = @IdCliente)
              AND (@Titulo IS NULL OR a.titulo ILIKE '%' || @Titulo || '%')
              AND (@Ativo IS NULL OR a.ativo = @Ativo)
            """;
        const string sqlItens = """
            SELECT a.id               AS "Id",
                   a.id_cliente       AS "IdCliente",
                   a.titulo           AS "Titulo",
                   a.descricao        AS "Descricao",
                   a.id_tipo_licenca  AS "IdTipoLicenca",
                   tl.descricao       AS "TipoLicencaDescricao",
                   a.ativo            AS "Ativo"
            FROM aplicacao a
            JOIN tipo_licenca tl ON tl.id = a.id_tipo_licenca
            WHERE (@IdCliente IS NULL OR a.id_cliente = @IdCliente)
              AND (@Titulo IS NULL OR a.titulo ILIKE '%' || @Titulo || '%')
              AND (@Ativo IS NULL OR a.ativo = @Ativo)
            ORDER BY a.titulo
            LIMIT @Limite OFFSET @Offset
            """;
        var param = new { IdCliente = idCliente, Titulo = titulo, Ativo = ativo,
                          Limite = tamanhoPagina, Offset = (pagina - 1) * tamanhoPagina };
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, param, cancellationToken: ct));
        var itens = (await conn.QueryAsync<AplicacaoResult>(
            new CommandDefinition(sqlItens, param, cancellationToken: ct))).AsList();
        return new PagedResult<AplicacaoResult>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Guid> InserirAsync(Domain.Entities.Aplicacao aplicacao, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO aplicacao (id, id_cliente, titulo, descricao, id_tipo_licenca, ativo)
            VALUES (@Id, @IdCliente, @Titulo, @Descricao, @IdTipoLicenca, TRUE)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = aplicacao.Id, IdCliente = aplicacao.IdCliente,
                  Titulo = aplicacao.Titulo, Descricao = aplicacao.Descricao,
                  IdTipoLicenca = aplicacao.IdTipoLicenca },
            cancellationToken: ct));
        return aplicacao.Id;
    }

    public async Task AtualizarAsync(AtualizarAplicacaoCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE aplicacao
               SET titulo    = @Titulo,
                   descricao = @Descricao
             WHERE id = @Id
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = command.Id, Titulo = command.Titulo, Descricao = command.Descricao },
            cancellationToken: ct));
    }

    public async Task DesativarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE aplicacao SET ativo = FALSE WHERE id = @Id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
