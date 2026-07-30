using Dapper;
using LicenciamentoSoftware.Application.Common;
using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class UsuarioGestaoRepository : IUsuarioGestaoRepository
{
    private readonly DbConnectionFactory _factory;

    public UsuarioGestaoRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<UsuarioResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT u.id              AS "Id",
                   u.id_cliente      AS "IdCliente",
                   u.nome            AS "Nome",
                   u.email           AS "Email",
                   COALESCE(p.papel, '') AS "Papel",
                   u.ativo           AS "Ativo"
            FROM usuario u
            LEFT JOIN usuario_papel p ON p.id_usuario = u.id
            WHERE u.id = @Id
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UsuarioResult>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExisteEmailAsync(
        string email, Guid? ignorarId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM usuario
                WHERE LOWER(email) = LOWER(@Email)
                  AND ativo = TRUE
                  AND (@IgnorarId IS NULL OR id <> @IgnorarId)
            )
            """;

        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql,
                new { Email = email, IgnorarId = ignorarId },
                cancellationToken: ct));
    }

    public async Task<PagedResult<UsuarioResult>> ListarAsync(
        Guid? idCliente, string? nome, bool? ativo,
        int pagina, int tamanhoPagina,
        CancellationToken ct = default)
    {
        const string sqlCount = """
            SELECT COUNT(*) FROM usuario u
            WHERE (@IdCliente IS NULL OR u.id_cliente = @IdCliente)
              AND (@Nome IS NULL OR u.nome ILIKE '%' || @Nome || '%')
              AND (@Ativo IS NULL OR u.ativo = @Ativo)
            """;

        const string sqlItens = """
            SELECT u.id              AS "Id",
                   u.id_cliente      AS "IdCliente",
                   u.nome            AS "Nome",
                   u.email           AS "Email",
                   COALESCE(p.papel, '') AS "Papel",
                   u.ativo           AS "Ativo"
            FROM usuario u
            LEFT JOIN usuario_papel p ON p.id_usuario = u.id
            WHERE (@IdCliente IS NULL OR u.id_cliente = @IdCliente)
              AND (@Nome IS NULL OR u.nome ILIKE '%' || @Nome || '%')
              AND (@Ativo IS NULL OR u.ativo = @Ativo)
            ORDER BY u.nome
            LIMIT @Limite OFFSET @Offset
            """;

        var param = new
        {
            IdCliente = idCliente,
            Nome = nome,
            Ativo = ativo,
            Limite = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        };

        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, param, cancellationToken: ct));
        var itens = (await conn.QueryAsync<UsuarioResult>(
            new CommandDefinition(sqlItens, param, cancellationToken: ct))).AsList();

        return new PagedResult<UsuarioResult>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Guid> InserirAsync(
        Domain.Entities.Usuario usuario, string papel, CancellationToken ct = default)
    {
        const string sqlUsuario = """
            INSERT INTO usuario (id, id_cliente, nome, email, senha_hash, ativo)
            VALUES (@Id, @IdCliente, @Nome, @Email, @SenhaHash, TRUE)
            """;

        const string sqlPapel = """
            INSERT INTO usuario_papel (id_usuario, papel)
            VALUES (@IdUsuario, @Papel)
            ON CONFLICT (id_usuario) DO UPDATE SET papel = EXCLUDED.papel
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sqlUsuario,
            new { Id = usuario.Id, IdCliente = usuario.IdCliente,
                  Nome = usuario.Nome, Email = usuario.Email,
                  SenhaHash = usuario.SenhaHash },
            cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(sqlPapel,
            new { IdUsuario = usuario.Id, Papel = papel },
            cancellationToken: ct));

        return usuario.Id;
    }

    public async Task AtualizarAsync(AtualizarUsuarioCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE usuario
               SET nome  = @Nome,
                   email = LOWER(@Email)
             WHERE id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { Id = command.Id, Nome = command.Nome, Email = command.Email },
            cancellationToken: ct));
    }

    public async Task DesativarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE usuario SET ativo = FALSE WHERE id = @Id";

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
