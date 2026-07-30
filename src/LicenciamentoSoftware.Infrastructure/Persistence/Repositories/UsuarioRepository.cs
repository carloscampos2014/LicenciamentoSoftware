using Dapper;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Leituras usam DbConnectionFactory diretamente (sem transação).
/// Escritas usam IUnitOfWork (com transação aberta pelo handler).
/// </summary>
public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly IUnitOfWork _uow;
    private readonly DbConnectionFactory _factory;

    public UsuarioRepository(IUnitOfWork uow, DbConnectionFactory factory)
    {
        _uow = uow;
        _factory = factory;
    }

    public async Task<Usuario?> BuscarPorEmailAsync(
        string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id          AS "Id",
                   u.id_cliente  AS "IdCliente",
                   u.nome        AS "Nome",
                   u.email       AS "Email",
                   u.senha_hash  AS "SenhaHash",
                   u.totp_secret_hash AS "TotpSecretHash",
                   u.ativo       AS "Ativo"
            FROM usuario u
            WHERE u.ativo = TRUE
              AND LOWER(u.email) = LOWER(@Email)
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<UsuarioRow>(
            new CommandDefinition(sql, new { Email = email },
                cancellationToken: cancellationToken));

        return row is null ? null : MapearUsuario(row);
    }

    public async Task<Usuario?> BuscarPorIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id          AS "Id",
                   id_cliente  AS "IdCliente",
                   nome        AS "Nome",
                   email       AS "Email",
                   senha_hash  AS "SenhaHash",
                   totp_secret_hash AS "TotpSecretHash",
                   ativo       AS "Ativo"
            FROM usuario
            WHERE id = @Id
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<UsuarioRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : MapearUsuario(row);
    }

    public async Task<string> BuscarPapelAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT papel FROM usuario_papel
            WHERE id_usuario = @IdUsuario
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { IdUsuario = idUsuario },
                cancellationToken: cancellationToken))
            ?? "OperadorCliente";
    }

    public async Task<bool> ExisteAdminParaClienteAsync(
        Guid idCliente, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM usuario u
            INNER JOIN usuario_papel up ON up.id_usuario = u.id
            WHERE u.id_cliente = @IdCliente
              AND up.papel = 'AdministradorCliente'
              AND u.ativo = TRUE
            """;

        using var conn = _factory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { IdCliente = idCliente },
                cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task SalvarAsync(
        Usuario usuario, string papel, CancellationToken cancellationToken = default)
    {
        const string sqlUsuario = """
            INSERT INTO usuario (id, id_cliente, nome, email, senha_hash, totp_secret_hash, ativo)
            VALUES (@Id, @IdCliente, @Nome, @Email, @SenhaHash, @TotpSecretHash, @Ativo)
            """;

        const string sqlPapel = """
            INSERT INTO usuario_papel (id_usuario, papel)
            VALUES (@IdUsuario, @Papel)
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sqlUsuario,
                new
                {
                    usuario.Id,
                    usuario.IdCliente,
                    usuario.Nome,
                    usuario.Email,
                    usuario.SenhaHash,
                    usuario.TotpSecretHash,
                    usuario.Ativo,
                },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sqlPapel,
                new { IdUsuario = usuario.Id, Papel = papel },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task AtualizarTotpSecretAsync(
        Guid idUsuario, string? totpSecret, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuario SET totp_secret_hash = @TotpSecret
            WHERE id = @IdUsuario
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { TotpSecret = totpSecret, IdUsuario = idUsuario },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    // ----- Mapeamento -----
#pragma warning disable CA1812
    private sealed class UsuarioRow
    {
        public Guid Id { get; set; }
        public Guid IdCliente { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? TotpSecretHash { get; set; }
        public bool Ativo { get; set; }
    }
#pragma warning restore CA1812

    private static Usuario MapearUsuario(UsuarioRow row)
    {
        var usuario = (Usuario)System.Runtime.CompilerServices
            .RuntimeHelpers.GetUninitializedObject(typeof(Usuario));

        SetProp(usuario, nameof(Usuario.Id), row.Id);
        SetProp(usuario, nameof(Usuario.IdCliente), row.IdCliente);
        SetProp(usuario, nameof(Usuario.Nome), row.Nome);
        SetProp(usuario, nameof(Usuario.Email), row.Email);
        SetProp(usuario, nameof(Usuario.SenhaHash), row.SenhaHash);
        SetProp(usuario, nameof(Usuario.TotpSecretHash), row.TotpSecretHash);
        SetProp(usuario, nameof(Usuario.Ativo), row.Ativo);

        return usuario;
    }

    private static void SetProp(object obj, string nome, object? valor)
    {
        var prop = typeof(Usuario).GetProperty(nome,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, valor);
    }
}
