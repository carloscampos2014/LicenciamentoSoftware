using Dapper;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly DbConnectionFactory _factory;

    public ClienteRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<ClienteResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id                  AS "Id",
                   razao_social        AS "RazaoSocial",
                   tipo_inscricao      AS "TipoInscricao",
                   numero_inscricao    AS "NumeroInscricao",
                   email               AS "Email",
                   telefone            AS "Telefone",
                   ativo               AS "Ativo"
            FROM cliente
            WHERE id = @Id
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ClienteResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExisteInscricaoAsync(
        int tipoInscricao, string numeroInscricao,
        Guid? ignorarId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM cliente
                WHERE tipo_inscricao   = @TipoInscricao
                  AND numero_inscricao = @NumeroInscricao
                  AND (@IgnorarId IS NULL OR id <> @IgnorarId)
            )
            """;

        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql,
                new { TipoInscricao = tipoInscricao, NumeroInscricao = numeroInscricao, IgnorarId = ignorarId },
                cancellationToken: ct));
    }

    public async Task<PagedResult<ClienteResult>> ListarAsync(
        string? razaoSocial, bool? ativo,
        int pagina, int tamanhoPagina,
        CancellationToken ct = default)
    {
        const string sqlCount = """
            SELECT COUNT(*) FROM cliente
            WHERE (@RazaoSocial IS NULL OR razao_social ILIKE '%' || @RazaoSocial || '%')
              AND (@Ativo IS NULL OR ativo = @Ativo)
            """;

        const string sqlItens = """
            SELECT id                  AS "Id",
                   razao_social        AS "RazaoSocial",
                   tipo_inscricao      AS "TipoInscricao",
                   numero_inscricao    AS "NumeroInscricao",
                   email               AS "Email",
                   telefone            AS "Telefone",
                   ativo               AS "Ativo"
            FROM cliente
            WHERE (@RazaoSocial IS NULL OR razao_social ILIKE '%' || @RazaoSocial || '%')
              AND (@Ativo IS NULL OR ativo = @Ativo)
            ORDER BY razao_social
            LIMIT @Limite OFFSET @Offset
            """;

        var param = new
        {
            RazaoSocial = razaoSocial,
            Ativo = ativo,
            Limite = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        };

        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, param, cancellationToken: ct));
        var itens = (await conn.QueryAsync<ClienteResult>(
            new CommandDefinition(sqlItens, param, cancellationToken: ct))).AsList();

        return new PagedResult<ClienteResult>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Guid> InserirAsync(
        Domain.Entities.Cliente cliente, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO cliente
                (id, razao_social, tipo_inscricao, numero_inscricao, email, telefone, ativo)
            VALUES
                (@Id, @RazaoSocial, @TipoInscricao, @NumeroInscricao, @Email, @Telefone, TRUE)
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new
            {
                Id              = cliente.Id,
                RazaoSocial     = cliente.RazaoSocial,
                TipoInscricao   = (int)cliente.Inscricao.Tipo,
                NumeroInscricao = cliente.Inscricao.Numero,
                Email           = cliente.Email.Endereco,
                Telefone        = cliente.Telefone?.Numero,
            },
            cancellationToken: ct));

        return cliente.Id;
    }

    public async Task AtualizarAsync(AtualizarClienteCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE cliente
               SET razao_social = @RazaoSocial,
                   email        = @Email,
                   telefone     = @Telefone
             WHERE id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new
            {
                Id          = command.Id,
                RazaoSocial = command.RazaoSocial,
                Email       = command.Email.ToLowerInvariant(),
                Telefone    = command.Telefone,
            },
            cancellationToken: ct));
    }

    public async Task DesativarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE cliente SET ativo = FALSE WHERE id = @Id";

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
