using Dapper;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Enums;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly IUnitOfWork _uow;

    public UsuarioRepository(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Usuario?> BuscarPorEmailAsync(
        string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, u.id_cliente, u.nome, u.senha_hash,
                   u.totp_secret_hash, u.ativo
            FROM usuario u
            INNER JOIN usuario_papel up ON up.id_usuario = u.id
            WHERE u.ativo = TRUE
              AND LOWER(u.email) = LOWER(@Email)
            LIMIT 1
            """;

        var row = await _uow.Connection.QueryFirstOrDefaultAsync<UsuarioRow>(
            new CommandDefinition(sql, new { Email = email },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));

        return row is null ? null : MapearUsuario(row);
    }

    public async Task<Usuario?> BuscarPorIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, id_cliente, nome, senha_hash, totp_secret_hash, ativo
            FROM usuario
            WHERE id = @Id
            LIMIT 1
            """;

        var row = await _uow.Connection.QueryFirstOrDefaultAsync<UsuarioRow>(
            new CommandDefinition(sql, new { Id = id },
                transaction: _uow.Transaction,
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

        return await _uow.Connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { IdUsuario = idUsuario },
                transaction: _uow.Transaction,
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

        var count = await _uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { IdCliente = idCliente },
                transaction: _uow.Transaction,
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
                    Email = string.Empty, // email vem do command, não da entidade nesta fase
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

    private sealed record UsuarioRow(
        Guid Id, Guid IdCliente, string Nome,
        string SenhaHash, string? TotpSecretHash, bool Ativo);

    private static Usuario MapearUsuario(UsuarioRow row)
    {
        // Reconstrói a entidade via reflection (necessário pois construtor é privado)
        var usuario = (Usuario)System.Runtime.CompilerServices
            .RuntimeHelpers.GetUninitializedObject(typeof(Usuario));

        SetProp(usuario, nameof(Usuario.Id), row.Id);
        SetProp(usuario, nameof(Usuario.IdCliente), row.IdCliente);
        SetProp(usuario, nameof(Usuario.Nome), row.Nome);
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
