using Dapper;
using LicenciamentoSoftware.Application.Licenca.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de leitura enriquecida para o fluxo de validação de licença.
/// Consolida licença + tipo + detalhe em uma única roundtrip ao banco.
/// </summary>
public sealed class ValidacaoLicencaRepository : IValidacaoLicencaRepository
{
    private readonly DbConnectionFactory _factory;

    public ValidacaoLicencaRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<LicencaValidacaoInfo?> BuscarParaValidacaoAsync(
        Guid idLicenca, CancellationToken ct = default)
    {
        // Query única que traz todos os campos necessários para a decisão de validação.
        // LEFT JOIN nos três tipos de detalhe — apenas um deles será não-nulo por licença.
        const string sql = """
            SELECT
                l.id                          AS "Id",
                l.id_cliente                  AS "IdCliente",
                l.ativo                       AS "Ativo",
                a.id_tipo_licenca             AS "IdTipoLicenca",
                -- Por Período
                lp.data_fim                   AS "DataFim",
                lp.renovacao_automatica       AS "RenovacaoAutomatica",
                -- Por Usuários
                lu.quantidade_maxima          AS "QuantidadeMaximaUsuarios",
                lu.max_sessoes_por_usuario    AS "MaxSessoesPorUsuario",
                lu.tempo_limite_sessao_horas  AS "TempoLimiteSessaoHoras",
                -- Por Instalação
                li.quantidade_maxima          AS "QuantidadeMaximaInstalacoes"
            FROM licenca l
            JOIN aplicacao a ON a.id = l.id_aplicativo
            LEFT JOIN licenca_periodo     lp ON lp.licenca_id = l.id
            LEFT JOIN licenca_usuarios    lu ON lu.licenca_id = l.id
            LEFT JOIN licenca_instalacao  li ON li.licenca_id = l.id
            WHERE l.id = @IdLicenca
            LIMIT 1
            """;

        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<LicencaValidacaoInfo>(
            new CommandDefinition(sql, new { IdLicenca = idLicenca }, cancellationToken: ct));
    }
}
