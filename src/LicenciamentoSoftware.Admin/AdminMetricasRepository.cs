using Dapper;
using LicenciamentoSoftware.Infrastructure.Persistence;

namespace LicenciamentoSoftware.Admin;

/// <summary>
/// Repositório direto para o painel Admin — consultas somente-leitura via Dapper.
/// Acessa todas as tabelas da plataforma para gerar métricas globais.
/// </summary>
public sealed class AdminMetricasRepository(DbConnectionFactory factory)
{
    // ─────────────────────────────────────────────────────────────────────────
    // Métricas de clientes / usuários
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<MetricasGerais> BuscarMetricasGeraisAsync()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*)               FROM cliente)                                        AS "TotalClientes",
                (SELECT COUNT(*)               FROM cliente WHERE ativo = TRUE)                     AS "ClientesAtivos",
                (SELECT COUNT(*)               FROM cliente WHERE encerrado_em IS NOT NULL)          AS "ClientesEncerrados",
                (SELECT COUNT(*)               FROM usuario WHERE ativo = TRUE)                     AS "UsuariosAtivos",
                (SELECT COUNT(*)               FROM licenca WHERE ativo = TRUE)                     AS "LicencasAtivas",
                (SELECT COUNT(*)               FROM licenca WHERE ativo = FALSE)                    AS "LicencasInativas",
                (SELECT COUNT(*)               FROM licenca WHERE ativo = TRUE
                                                               AND tipo_licenca_id IN (
                                                                   SELECT id FROM tipo_licenca WHERE descricao = 'Por Período')
                                                               AND data_fim <= NOW() + INTERVAL '7 days'
                                                               AND data_fim >= NOW())               AS "LicencasExpirandoEm7Dias",
                (SELECT COUNT(*)               FROM licenca_sessao WHERE ativo = TRUE)              AS "SessoesAbertas",
                (SELECT COUNT(*)               FROM validacao_log
                                               WHERE criado_em >= NOW() - INTERVAL '24 hours')      AS "ValidacoesUltimas24h",
                (SELECT COUNT(*)               FROM validacao_log
                                               WHERE criado_em >= NOW() - INTERVAL '7 days')        AS "ValidacoesUltimos7Dias",
                (SELECT COUNT(*)               FROM validacao_log
                                               WHERE resultado = 'Erro'
                                               AND criado_em >= NOW() - INTERVAL '24 hours')        AS "ErrosUltimas24h"
            """;

        using var conn = factory.CreateConnection();
        return await conn.QueryFirstAsync<MetricasGerais>(sql);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Erros de validação por motivo (últimas 24h)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ErroMotivo>> BuscarErrosPorMotivoAsync()
    {
        const string sql = """
            SELECT motivo_erro  AS "Motivo",
                   COUNT(*)     AS "Total"
            FROM validacao_log
            WHERE resultado    = 'Erro'
              AND criado_em   >= NOW() - INTERVAL '24 hours'
              AND motivo_erro IS NOT NULL
            GROUP BY motivo_erro
            ORDER BY Total DESC
            LIMIT 10
            """;

        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<ErroMotivo>(sql)).AsList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Últimos logins (hora + IP)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<UltimoLogin>> BuscarUltimosLoginsAsync()
    {
        const string sql = """
            SELECT u.email       AS "Email",
                   vl.ip_origem  AS "Ip",
                   vl.criado_em  AS "HoraUtc"
            FROM validacao_log vl
            INNER JOIN licenca l ON l.id = vl.id_licenca
            INNER JOIN usuario u ON u.id_cliente = l.id_cliente AND u.ativo = TRUE
            WHERE vl.tipo_operacao = 'Login'
              AND vl.resultado     = 'Sucesso'
            ORDER BY vl.criado_em DESC
            LIMIT 20
            """;

        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<UltimoLogin>(sql)).AsList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tamanho do banco de dados
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> BuscarTamanhoBancoAsync()
    {
        const string sql = "SELECT pg_size_pretty(pg_database_size(current_database()))";
        using var conn = factory.CreateConnection();
        return await conn.ExecuteScalarAsync<string>(sql) ?? "N/D";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Licenças ativas por tipo
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LicencaPorTipo>> BuscarLicencasPorTipoAsync()
    {
        const string sql = """
            SELECT tl.descricao AS "Tipo",
                   COUNT(*)     AS "Total"
            FROM licenca l
            INNER JOIN tipo_licenca tl ON tl.id = l.tipo_licenca_id
            WHERE l.ativo = TRUE
            GROUP BY tl.descricao
            ORDER BY Total DESC
            """;

        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LicencaPorTipo>(sql)).AsList();
    }
}

// ── Modelos de resultado ──────────────────────────────────────────────────────

public sealed record MetricasGerais(
    int TotalClientes,
    int ClientesAtivos,
    int ClientesEncerrados,
    int UsuariosAtivos,
    int LicencasAtivas,
    int LicencasInativas,
    int LicencasExpirandoEm7Dias,
    int SessoesAbertas,
    long ValidacoesUltimas24h,
    long ValidacoesUltimos7Dias,
    long ErrosUltimas24h);

public sealed record ErroMotivo(string Motivo, long Total);
public sealed record LicencaPorTipo(string Tipo, long Total);
public sealed record UltimoLogin(string Email, string? Ip, DateTime HoraUtc);
