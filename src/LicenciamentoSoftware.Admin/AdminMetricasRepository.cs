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
                (SELECT COUNT(*)  FROM cliente)                                                     AS "TotalClientes",
                (SELECT COUNT(*)  FROM cliente WHERE ativo = TRUE)                                  AS "ClientesAtivos",
                (SELECT COUNT(*)  FROM cliente WHERE encerrado_em IS NOT NULL)                       AS "ClientesEncerrados",
                (SELECT COUNT(*)  FROM usuario WHERE ativo = TRUE)                                  AS "UsuariosAtivos",
                (SELECT COUNT(*)  FROM licenca WHERE ativo = TRUE)                                  AS "LicencasAtivas",
                (SELECT COUNT(*)  FROM licenca WHERE ativo = FALSE)                                 AS "LicencasInativas",
                (SELECT COUNT(*)  FROM licenca l
                                  INNER JOIN licenca_periodo lp ON lp.licenca_id = l.id
                                  WHERE l.ativo = TRUE
                                    AND lp.data_fim <= NOW() + INTERVAL '7 days'
                                    AND lp.data_fim >= NOW())                                       AS "LicencasExpirandoEm7Dias",
                (SELECT COUNT(*)  FROM licenca_sessao WHERE ativo = TRUE)                           AS "SessoesAbertas",
                (SELECT COUNT(*)  FROM validacao_log
                                  WHERE criado_em >= NOW() - INTERVAL '24 hours')                   AS "ValidacoesUltimas24h",
                (SELECT COUNT(*)  FROM validacao_log
                                  WHERE criado_em >= NOW() - INTERVAL '7 days')                     AS "ValidacoesUltimos7Dias",
                (SELECT COUNT(*)  FROM validacao_log
                                  WHERE resultado = 'erro'
                                    AND criado_em >= NOW() - INTERVAL '24 hours')                   AS "ErrosUltimas24h"
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
            WHERE resultado    = 'erro'
              AND criado_em   >= NOW() - INTERVAL '24 hours'
              AND motivo_erro IS NOT NULL
            GROUP BY motivo_erro
            ORDER BY 2 DESC
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
            SELECT c.razao_social  AS "Email",
                   vl.ip_origem   AS "Ip",
                   vl.criado_em   AS "HoraUtc"
            FROM validacao_log vl
            INNER JOIN licenca l  ON l.id           = vl.id_licenca
            INNER JOIN cliente c  ON c.id           = l.id_cliente
            WHERE vl.tipo_operacao = 'login'
              AND vl.resultado     = 'sucesso'
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
            SELECT
                CASE
                    WHEN lp.licenca_id IS NOT NULL THEN 'Por Período'
                    WHEN lu.licenca_id IS NOT NULL THEN 'Por Usuários'
                    WHEN li.licenca_id IS NOT NULL THEN 'Por Instalação'
                    ELSE 'Permanente'
                END                AS "Tipo",
                COUNT(*)           AS "Total"
            FROM licenca l
            LEFT JOIN licenca_periodo     lp ON lp.licenca_id = l.id
            LEFT JOIN licenca_usuarios    lu ON lu.licenca_id = l.id
            LEFT JOIN licenca_instalacao  li ON li.licenca_id = l.id
            WHERE l.ativo = TRUE
            GROUP BY 1
            ORDER BY 2 DESC
            """;

        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<LicencaPorTipo>(sql)).AsList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Listagem de usuários com status 2FA (para reset via Admin)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<UsuarioAdminInfo>> ListarUsuariosAsync()
    {
        const string sql = """
            SELECT u.id           AS "Id",
                   u.nome         AS "Nome",
                   u.email        AS "Email",
                   u.ativo        AS "Ativo",
                   c.razao_social AS "NomeCliente",
                   up.papel       AS "Papel",
                   CASE WHEN u.totp_secret_hash IS NOT NULL THEN TRUE ELSE FALSE END AS "TotpAtivo"
            FROM usuario u
            INNER JOIN cliente c ON c.id = u.id_cliente
            LEFT  JOIN usuario_papel up ON up.id_usuario = u.id
            ORDER BY c.razao_social, u.nome
            """;

        using var conn = factory.CreateConnection();
        return (await conn.QueryAsync<UsuarioAdminInfo>(sql)).AsList();
    }
}

// ── Modelos de resultado ──────────────────────────────────────────────────────
public sealed record MetricasGerais(
    long TotalClientes,
    long ClientesAtivos,
    long ClientesEncerrados,
    long UsuariosAtivos,
    long LicencasAtivas,
    long LicencasInativas,
    long LicencasExpirandoEm7Dias,
    long SessoesAbertas,
    long ValidacoesUltimas24h,
    long ValidacoesUltimos7Dias,
    long ErrosUltimas24h);

public sealed record ErroMotivo(string Motivo, long Total);
public sealed record LicencaPorTipo(string Tipo, long Total);
public sealed record UltimoLogin(string Email, string? Ip, DateTime HoraUtc);
public sealed record UsuarioAdminInfo(
    Guid Id, string Nome, string Email, bool Ativo,
    string NomeCliente, string? Papel, bool TotpAtivo);
