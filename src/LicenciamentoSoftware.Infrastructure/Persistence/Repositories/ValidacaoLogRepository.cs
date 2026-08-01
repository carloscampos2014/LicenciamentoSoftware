using Dapper;
using LicenciamentoSoftware.Application.Licenca.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de escrita para o log de validações.
/// Usa conexão direta (fora de UnitOfWork) para garantir que o log
/// seja gravado mesmo quando a transação principal é revertida.
/// </summary>
public sealed class ValidacaoLogRepository(DbConnectionFactory factory) : IValidacaoLogRepository
{
    public async Task InserirAsync(
        Guid idLicenca,
        string tipoOperacao,
        string resultado,
        string? motivoErro = null,
        string? ipOrigem = null,
        CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO validacao_log
                (id, id_licenca, tipo_operacao, resultado, motivo_erro, ip_origem, criado_em)
            VALUES
                (uuid_generate_v4(), @IdLicenca, @TipoOperacao, @Resultado,
                 @MotivoErro, @IpOrigem, NOW())
            """;
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { IdLicenca = idLicenca, TipoOperacao = tipoOperacao,
                  Resultado = resultado, MotivoErro = motivoErro, IpOrigem = ipOrigem },
            cancellationToken: ct));
    }
}
