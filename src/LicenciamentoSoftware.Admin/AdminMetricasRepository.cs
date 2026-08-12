using Dapper;
using LicenciamentoSoftware.Infrastructure.Persistence;
using System.Data;

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
    // Solicitações de reset de 2FA pendentes (contagem para o dashboard)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<int> ContarReset2FAPendentesAsync()
    {
        const string sql = "SELECT COUNT(*) FROM solicitacao_reset_2fa WHERE status = 'Pendente'";
        using var conn = factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql);
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

    // ─────────────────────────────────────────────────────────────────────────
    // Histórico de validações (paginado, com filtros)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<ValidacaoLogItem> Itens, long Total)> ListarValidacoesAsync(
        int pagina, int tamanhoPagina,
        string? resultado, string? tipoOperacao, string? motivo,
        DateTime? dataInicio, DateTime? dataFim)
    {
        // SQL dinâmico — evita 42P08 do Npgsql com parâmetros nullable
        // em cláusulas IS NULL + comparação na mesma expressão
        var where = new List<string>();
        var dp    = new DynamicParameters();

        if (!string.IsNullOrEmpty(resultado))
        {
            where.Add("vl.resultado = @Resultado");
            dp.Add("Resultado", resultado, DbType.String);
        }
        if (!string.IsNullOrEmpty(tipoOperacao))
        {
            where.Add("vl.tipo_operacao = @TipoOperacao");
            dp.Add("TipoOperacao", tipoOperacao, DbType.String);
        }
        if (!string.IsNullOrEmpty(motivo))
        {
            where.Add("vl.motivo_erro = @Motivo");
            dp.Add("Motivo", motivo, DbType.String);
        }
        if (dataInicio.HasValue)
        {
            where.Add("vl.criado_em >= @DataInicio");
            dp.Add("DataInicio", dataInicio.Value, DbType.DateTime);
        }
        if (dataFim.HasValue)
        {
            where.Add("vl.criado_em <= @DataFim");
            dp.Add("DataFim", dataFim.Value, DbType.DateTime);
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        dp.Add("Limite", tamanhoPagina,              DbType.Int32);
        dp.Add("Offset", (pagina - 1) * tamanhoPagina, DbType.Int32);

        var sqlCount = $"""
            SELECT COUNT(*)
            FROM validacao_log vl
            LEFT JOIN licenca l  ON l.id = vl.id_licenca
            LEFT JOIN cliente c  ON c.id = l.id_cliente
            {whereClause}
            """;

        var sqlItens = $"""
            SELECT
                vl.id               AS "Id",
                vl.criado_em        AS "CriadoEm",
                COALESCE(c.razao_social, '—')   AS "NomeCliente",
                COALESCE(a.titulo,       '—')   AS "AplicativoTitulo",
                COALESCE(cf.razao_social,'—')   AS "ClienteFinalRazaoSocial",
                vl.tipo_operacao    AS "TipoOperacao",
                vl.resultado        AS "Resultado",
                vl.motivo_erro      AS "MotivoErro",
                vl.ip_origem        AS "IpOrigem"
            FROM validacao_log vl
            LEFT JOIN licenca l       ON l.id  = vl.id_licenca
            LEFT JOIN cliente c       ON c.id  = l.id_cliente
            LEFT JOIN aplicacao a     ON a.id  = l.id_aplicativo
            LEFT JOIN cliente_final cf ON cf.id = l.id_cliente_final
            {whereClause}
            ORDER BY vl.criado_em DESC
            LIMIT @Limite OFFSET @Offset
            """;

        using var conn = factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(sqlCount, dp);
        var itens = (await conn.QueryAsync<ValidacaoLogItem>(sqlItens, dp)).AsList();
        return (itens, total);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sessões ativas (paginado)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<SessaoAtivaItem> Itens, long Total)> ListarSessoesAtivasAsync(
        int pagina, int tamanhoPagina, string? filtro)
    {
        const string sqlCount = """
            SELECT COUNT(*)
            FROM licenca_sessao ls
            INNER JOIN licenca l      ON l.id  = ls.licenca_id
            INNER JOIN cliente c      ON c.id  = l.id_cliente
            INNER JOIN aplicacao a    ON a.id  = l.id_aplicativo
            INNER JOIN cliente_final cf ON cf.id = l.id_cliente_final
            WHERE ls.ativo = TRUE
              AND (@Filtro IS NULL
                   OR ls.identificador_usuario ILIKE '%' || @Filtro || '%'
                   OR cf.razao_social           ILIKE '%' || @Filtro || '%'
                   OR a.titulo                  ILIKE '%' || @Filtro || '%')
            """;

        const string sqlItens = """
            SELECT
                ls.id                    AS "Id",
                l.id                     AS "IdLicenca",
                c.razao_social           AS "NomeCliente",
                cf.razao_social          AS "ClienteFinalRazaoSocial",
                a.titulo                 AS "AplicativoTitulo",
                ls.identificador_usuario AS "IdentificadorUsuario",
                ls.data_login            AS "DataLogin",
                ls.data_ultima_atividade AS "DataUltimaAtividade"
            FROM licenca_sessao ls
            INNER JOIN licenca l      ON l.id  = ls.licenca_id
            INNER JOIN cliente c      ON c.id  = l.id_cliente
            INNER JOIN aplicacao a    ON a.id  = l.id_aplicativo
            INNER JOIN cliente_final cf ON cf.id = l.id_cliente_final
            WHERE ls.ativo = TRUE
              AND (@Filtro IS NULL
                   OR ls.identificador_usuario ILIKE '%' || @Filtro || '%'
                   OR cf.razao_social           ILIKE '%' || @Filtro || '%'
                   OR a.titulo                  ILIKE '%' || @Filtro || '%')
            ORDER BY ls.data_ultima_atividade DESC
            LIMIT @Limite OFFSET @Offset
            """;

        var param = new
        {
            Filtro = string.IsNullOrWhiteSpace(filtro) ? null : filtro.Trim(),
            Limite = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        };

        using var conn = factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(sqlCount, param);
        var itens = (await conn.QueryAsync<SessaoAtivaItem>(sqlItens, param)).AsList();
        return (itens, total);
    }

    public async Task EncerrarSessaoAsync(Guid idSessao)
    {
        const string sql = "UPDATE licenca_sessao SET ativo = FALSE WHERE id = @Id";
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = idSessao });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Instalações registradas ativas (paginado)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<InstalacaoAtivaItem> Itens, long Total)> ListarInstalacoesAtivasAsync(
        int pagina, int tamanhoPagina, string? filtro)
    {
        const string sqlCount = """
            SELECT COUNT(*)
            FROM licenca_instalacao_registrada lir
            INNER JOIN licenca l      ON l.id  = lir.licenca_id
            INNER JOIN cliente c      ON c.id  = l.id_cliente
            INNER JOIN aplicacao a    ON a.id  = l.id_aplicativo
            INNER JOIN cliente_final cf ON cf.id = l.id_cliente_final
            WHERE lir.ativo = TRUE
              AND (@Filtro IS NULL
                   OR lir.identificador_maquina ILIKE '%' || @Filtro || '%'
                   OR cf.razao_social            ILIKE '%' || @Filtro || '%'
                   OR a.titulo                   ILIKE '%' || @Filtro || '%')
            """;

        const string sqlItens = """
            SELECT
                lir.id                        AS "Id",
                l.id                          AS "IdLicenca",
                c.razao_social                AS "NomeCliente",
                cf.razao_social               AS "ClienteFinalRazaoSocial",
                a.titulo                      AS "AplicativoTitulo",
                lir.identificador_maquina     AS "IdentificadorMaquina",
                lir.data_registro             AS "DataRegistro",
                lir.data_ultima_validacao     AS "DataUltimaValidacao"
            FROM licenca_instalacao_registrada lir
            INNER JOIN licenca l      ON l.id  = lir.licenca_id
            INNER JOIN cliente c      ON c.id  = l.id_cliente
            INNER JOIN aplicacao a    ON a.id  = l.id_aplicativo
            INNER JOIN cliente_final cf ON cf.id = l.id_cliente_final
            WHERE lir.ativo = TRUE
              AND (@Filtro IS NULL
                   OR lir.identificador_maquina ILIKE '%' || @Filtro || '%'
                   OR cf.razao_social            ILIKE '%' || @Filtro || '%'
                   OR a.titulo                   ILIKE '%' || @Filtro || '%')
            ORDER BY lir.data_registro DESC
            LIMIT @Limite OFFSET @Offset
            """;

        var param = new
        {
            Filtro = string.IsNullOrWhiteSpace(filtro) ? null : filtro.Trim(),
            Limite = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        };

        using var conn = factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<long>(sqlCount, param);
        var itens = (await conn.QueryAsync<InstalacaoAtivaItem>(sqlItens, param)).AsList();
        return (itens, total);
    }

    public async Task LiberarInstalacaoAsync(Guid idInstalacao)
    {
        const string sql = "UPDATE licenca_instalacao_registrada SET ativo = FALSE WHERE id = @Id";
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = idInstalacao });
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

public sealed record ValidacaoLogItem(
    Guid Id,
    DateTime CriadoEm,
    string NomeCliente,
    string AplicativoTitulo,
    string ClienteFinalRazaoSocial,
    string TipoOperacao,
    string Resultado,
    string? MotivoErro,
    string? IpOrigem);

public sealed record SessaoAtivaItem(
    Guid Id,
    Guid IdLicenca,
    string NomeCliente,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorUsuario,
    DateTime DataLogin,
    DateTime DataUltimaAtividade);

public sealed record InstalacaoAtivaItem(
    Guid Id,
    Guid IdLicenca,
    string NomeCliente,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorMaquina,
    DateTime DataRegistro,
    DateTime? DataUltimaValidacao);
