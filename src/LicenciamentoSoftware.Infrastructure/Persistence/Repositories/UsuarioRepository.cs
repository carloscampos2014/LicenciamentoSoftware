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
            INSERT INTO usuario (id, id_cliente, nome, email, senha_hash, totp_secret_hash, ativo,
                                 lgpd_aceito, lgpd_aceito_em, lgpd_ip_origem)
            VALUES (@Id, @IdCliente, @Nome, @Email, @SenhaHash, @TotpSecretHash, @Ativo,
                    @LgpdAceito, @LgpdAceitoEm, @LgpdIpOrigem)
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
                    usuario.LgpdAceito,
                    usuario.LgpdAceitoEm,
                    usuario.LgpdIpOrigem,
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

    // -------------------------------------------------------------------------
    // Fase 8 — jobs de notificação
    // -------------------------------------------------------------------------

    public async Task<Application.Jobs.AdminClienteInfo?> BuscarEmailAdminPorClienteAsync(
        Guid idCliente, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id_cliente AS "IdCliente",
                   u.email      AS "Email",
                   u.nome       AS "Nome"
            FROM usuario u
            INNER JOIN usuario_papel up ON up.id_usuario = u.id
            WHERE u.id_cliente = @IdCliente
              AND up.papel     = 'AdministradorCliente'
              AND u.ativo      = TRUE
            ORDER BY u.nome
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Application.Jobs.AdminClienteInfo>(
            new CommandDefinition(sql, new { IdCliente = idCliente },
                cancellationToken: cancellationToken));
    }

    // -------------------------------------------------------------------------
    // LGPD Art. 18 — anonimização de dados pessoais
    // -------------------------------------------------------------------------

    public async Task AnonimizarAsync(
        Guid idUsuario, string nomeSubstituto, string emailSubstituto,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuario
            SET nome             = @Nome,
                email            = @Email,
                senha_hash       = '',
                totp_secret_hash = NULL
            WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { Id = idUsuario, Nome = nomeSubstituto, Email = emailSubstituto },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task RevogarTodosRefreshTokensAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_token
            SET revogado = TRUE
            WHERE id_usuario = @IdUsuario
              AND revogado   = FALSE
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { IdUsuario = idUsuario },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task RevogarTodosRefreshTokensPorClienteAsync(
        Guid idCliente, CancellationToken cancellationToken = default)
    {
        // Revoga todos os refresh tokens ativos de todos os usuários do tenant.
        // Usado no encerramento de conta de empresa para bloquear qualquer renovação de sessão.
        const string sql = """
            UPDATE refresh_token rt
               SET revogado = TRUE
              FROM usuario u
             WHERE rt.id_usuario = u.id
               AND u.id_cliente  = @IdCliente
               AND rt.revogado   = FALSE
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { IdCliente = idCliente },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task DesativarTodosPorClienteAsync(
        Guid idCliente, CancellationToken cancellationToken = default)
    {
        // Desativa todos os usuários do tenant para bloquear novos logins após encerramento de conta.
        const string sql = """
            UPDATE usuario
               SET ativo = FALSE
             WHERE id_cliente = @IdCliente
               AND ativo      = TRUE
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { IdCliente = idCliente },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task DesativarUsuarioAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuario SET ativo = FALSE WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { Id = idUsuario },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task DefinirSenhaAsync(
        Guid idUsuario, string senhaHash, CancellationToken cancellationToken = default)
    {
        // Define nova senha sem exigir a anterior.
        // Usado no fluxo de recuperação após anonimização LGPD.
        const string sql = """
            UPDATE usuario SET senha_hash = @SenhaHash WHERE id = @Id
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { Id = idUsuario, SenhaHash = senhaHash },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task SalvarTotpPendenteAsync(
        Guid idUsuario, string segredo, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuario SET totp_secret_pendente = @Segredo WHERE id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new { Id = idUsuario, Segredo = segredo },
                cancellationToken: cancellationToken));
    }

    public async Task<string?> BuscarTotpPendenteAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT totp_secret_pendente FROM usuario WHERE id = @Id LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string?>(
            new CommandDefinition(sql,
                new { Id = idUsuario },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> ConfirmarTotpPendenteAsync(
        Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuario
            SET totp_secret_hash     = totp_secret_pendente,
                totp_secret_pendente = NULL
            WHERE id = @Id
              AND totp_secret_pendente IS NOT NULL
            """;

        using var conn = _factory.CreateConnection();
        var linhas = await conn.ExecuteAsync(
            new CommandDefinition(sql,
                new { Id = idUsuario },
                cancellationToken: cancellationToken));

        return linhas > 0;
    }

    public async Task<bool> ExisteOutroAdminAsync(
        Guid idCliente, Guid idUsuarioExcluindo, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM usuario u
            INNER JOIN usuario_papel up ON up.id_usuario = u.id
            WHERE u.id_cliente = @IdCliente
              AND up.papel     = 'AdministradorCliente'
              AND u.ativo      = TRUE
              AND u.id        <> @IdUsuarioExcluindo
            """;

        using var conn = _factory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql,
                new { IdCliente = idCliente, IdUsuarioExcluindo = idUsuarioExcluindo },
                cancellationToken: cancellationToken));

        return count > 0;
    }
}
