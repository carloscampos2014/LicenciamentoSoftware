using Dapper;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Commands;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class ClienteFinalRepository : IClienteFinalRepository
{
    private readonly DbConnectionFactory _factory;

    public ClienteFinalRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<ClienteFinalResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                AS "Id",
                   id_cliente        AS "IdCliente",
                   razao_social      AS "RazaoSocial",
                   tipo_inscricao    AS "TipoInscricao",
                   numero_inscricao  AS "NumeroInscricao",
                   email             AS "Email",
                   telefone          AS "Telefone",
                   ativo             AS "Ativo"
            FROM cliente_final
            WHERE id = @Id
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ClienteFinalResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExisteInscricaoAsync(
        Guid idCliente, int tipoInscricao, string numeroInscricao,
        Guid? ignorarId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM cliente_final
                WHERE id_cliente       = @IdCliente
                  AND tipo_inscricao   = @TipoInscricao
                  AND numero_inscricao = @NumeroInscricao
                  AND (@IgnorarId IS NULL OR id <> @IgnorarId)
            )
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql,
                new { IdCliente = idCliente, TipoInscricao = tipoInscricao,
                      NumeroInscricao = numeroInscricao, IgnorarId = ignorarId },
                cancellationToken: ct));
    }

    public async Task<PagedResult<ClienteFinalResult>> ListarAsync(
        Guid? idCliente, string? razaoSocial, bool? ativo,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        const string sqlCount = """
            SELECT COUNT(*) FROM cliente_final
            WHERE (@IdCliente IS NULL OR id_cliente = @IdCliente)
              AND (@RazaoSocial IS NULL OR razao_social ILIKE '%' || @RazaoSocial || '%')
              AND (@Ativo IS NULL OR ativo = @Ativo)
            """;
        const string sqlItens = """
            SELECT id                AS "Id",
                   id_cliente        AS "IdCliente",
                   razao_social      AS "RazaoSocial",
                   tipo_inscricao    AS "TipoInscricao",
                   numero_inscricao  AS "NumeroInscricao",
                   email             AS "Email",
                   telefone          AS "Telefone",
                   ativo             AS "Ativo"
            FROM cliente_final
            WHERE (@IdCliente IS NULL OR id_cliente = @IdCliente)
              AND (@RazaoSocial IS NULL OR razao_social ILIKE '%' || @RazaoSocial || '%')
              AND (@Ativo IS NULL OR ativo = @Ativo)
            ORDER BY razao_social
            LIMIT @Limite OFFSET @Offset
            """;
        var param = new { IdCliente = idCliente, RazaoSocial = razaoSocial, Ativo = ativo,
                          Limite = tamanhoPagina, Offset = (pagina - 1) * tamanhoPagina };
        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, param, cancellationToken: ct));
        var itens = (await conn.QueryAsync<ClienteFinalResult>(
            new CommandDefinition(sqlItens, param, cancellationToken: ct))).AsList();
        return new PagedResult<ClienteFinalResult>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Guid> InserirAsync(Domain.Entities.ClienteFinal clienteFinal, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO cliente_final
                (id, id_cliente, razao_social, tipo_inscricao, numero_inscricao, email, telefone, ativo)
            VALUES
                (@Id, @IdCliente, @RazaoSocial, @TipoInscricao, @NumeroInscricao, @Email, @Telefone, TRUE)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = clienteFinal.Id, IdCliente = clienteFinal.IdCliente, RazaoSocial = clienteFinal.RazaoSocial,
                  TipoInscricao = (int)clienteFinal.Inscricao.Tipo, NumeroInscricao = clienteFinal.Inscricao.Numero,
                  Email = clienteFinal.Email.Endereco, Telefone = clienteFinal.Telefone?.Numero },
            cancellationToken: ct));
        return clienteFinal.Id;
    }

    public async Task AtualizarAsync(AtualizarClienteFinalCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE cliente_final
               SET razao_social = @RazaoSocial,
                   email        = @Email,
                   telefone     = @Telefone
             WHERE id = @Id
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = command.Id, RazaoSocial = command.RazaoSocial,
                  Email = command.Email.ToLowerInvariant(), Telefone = command.Telefone },
            cancellationToken: ct));
    }

    public async Task DesativarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE cliente_final SET ativo = FALSE WHERE id = @Id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
