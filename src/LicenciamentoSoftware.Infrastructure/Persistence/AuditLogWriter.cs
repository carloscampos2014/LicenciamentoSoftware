using Dapper;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Infrastructure.Persistence;

/// <summary>
/// Persiste LogOperacao na mesma transação corrente do UnitOfWork.
/// CamposAlterados é armazenado como JSONB no PostgreSQL.
/// </summary>
public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly IUnitOfWork _uow;

    public AuditLogWriter(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task RegistrarAsync(
        LogOperacao log, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO log_operacao
                (id, entidade, id_registro, operacao, data_hora, id_usuario, campos_alterados)
            VALUES
                (@Id, @Entidade, @IdRegistro, @Operacao::char,
                 @DataHora, @IdUsuario, @CamposAlterados::jsonb)
            """;

        await _uow.Connection.ExecuteAsync(
            new CommandDefinition(sql,
                new
                {
                    log.Id,
                    log.Entidade,
                    log.IdRegistro,
                    Operacao = ((char)log.Operacao).ToString(),
                    log.DataHora,
                    log.IdUsuario,
                    CamposAlterados = log.CamposAlterados ?? "null",
                },
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken));
    }
}
