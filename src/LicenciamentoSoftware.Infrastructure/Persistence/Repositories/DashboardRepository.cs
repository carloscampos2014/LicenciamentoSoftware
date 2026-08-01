using Dapper;
using LicenciamentoSoftware.Application.Dashboard.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de leitura para métricas do dashboard.
/// Usa CTEs PostgreSQL para retornar todas as métricas em uma única roundtrip.
/// </summary>
public sealed class DashboardRepository(DbConnectionFactory factory) : IDashboardRepository
{
    // UUIDs dos tipos de licença (seed V001)
    private static readonly Guid TipoPermanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoPeriodo    = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TipoUsuarios   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TipoInstalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public async Task<DashboardResumoResult> BuscarResumoAsync(
        Guid idCliente, CancellationToken ct = default)
    {
        const string sql = """
            WITH
            clientes_finais AS (
                SELECT COUNT(*) FILTER (WHERE ativo = TRUE) AS ativos
                FROM cliente_final
                WHERE id_cliente = @IdCliente
            ),
            aplicacoes AS (
                SELECT COUNT(*) FILTER (WHERE ativo = TRUE) AS ativas
                FROM aplicacao
                WHERE id_cliente = @IdCliente
            ),
            licencas AS (
                SELECT
                    COUNT(*) FILTER (WHERE l.ativo = TRUE)  AS ativas,
                    COUNT(*) FILTER (WHERE l.ativo = FALSE) AS inativas,
                    COUNT(*) FILTER (WHERE l.ativo = TRUE AND a.id_tipo_licenca = @TipoPermanente) AS permanente,
                    COUNT(*) FILTER (WHERE l.ativo = TRUE AND a.id_tipo_licenca = @TipoPeriodo)    AS por_periodo,
                    COUNT(*) FILTER (WHERE l.ativo = TRUE AND a.id_tipo_licenca = @TipoUsuarios)   AS por_usuarios,
                    COUNT(*) FILTER (WHERE l.ativo = TRUE AND a.id_tipo_licenca = @TipoInstalacao) AS por_instalacao
                FROM licenca l
                JOIN aplicacao a ON a.id = l.id_aplicativo
                WHERE l.id_cliente = @IdCliente
            ),
            expirando AS (
                SELECT COUNT(*) AS total
                FROM licenca l
                JOIN licenca_periodo lp ON lp.licenca_id = l.id
                WHERE l.id_cliente = @IdCliente
                  AND l.ativo = TRUE
                  AND lp.data_fim BETWEEN NOW() AND NOW() + INTERVAL '7 days'
            ),
            sessoes AS (
                SELECT COUNT(*) AS ativas
                FROM licenca_sessao ls
                JOIN licenca l ON l.id = ls.licenca_id
                WHERE l.id_cliente = @IdCliente AND ls.ativo = TRUE
            ),
            tokens AS (
                SELECT COUNT(*) AS expirando
                FROM licenca_token lt
                JOIN licenca l ON l.id = lt.id_licenca
                WHERE l.id_cliente = @IdCliente
                  AND lt.ativo = TRUE
                  AND (lt.criado_em + (lt.expiracao_minutos * INTERVAL '1 minute'))
                      BETWEEN NOW() AND NOW() + INTERVAL '7 days'
            ),
            novas_licencas AS (
                SELECT COUNT(*) AS total
                FROM licenca
                WHERE id_cliente = @IdCliente
                  AND data_cadastro >= NOW() - INTERVAL '30 days'
            ),
            novos_clientes AS (
                SELECT COUNT(*) AS total
                FROM cliente_final
                WHERE id_cliente = @IdCliente
                  AND data_cadastro >= NOW() - INTERVAL '30 days'
            )
            SELECT
                cf.ativos                  AS "ClientesFinaisAtivos",
                ap.ativas                  AS "AplicacoesAtivas",
                l.ativas                   AS "LicencasAtivas",
                l.inativas                 AS "LicencasInativas",
                l.permanente               AS "Permanente",
                l.por_periodo              AS "PorPeriodo",
                l.por_usuarios             AS "PorUsuarios",
                l.por_instalacao           AS "PorInstalacao",
                ex.total                   AS "LicencasExpirandoEm7Dias",
                s.ativas                   AS "SessoesAtivasAgora",
                tk.expirando               AS "TokensExpirandoEm7Dias",
                nl.total                   AS "NovasLicencasUltimos30Dias",
                nc.total                   AS "NovosClientesFinaisUltimos30Dias"
            FROM clientes_finais cf, aplicacoes ap, licencas l,
                 expirando ex, sessoes s, tokens tk,
                 novas_licencas nl, novos_clientes nc
            """;

        using var conn = factory.CreateConnection();
        var row = await conn.QuerySingleAsync<ResumoRaw>(
            new CommandDefinition(sql,
                new { IdCliente = idCliente,
                      TipoPermanente, TipoPeriodo,
                      TipoUsuarios, TipoInstalacao },
                cancellationToken: ct));

        return new DashboardResumoResult(
            TotalClientesFinaisAtivos:        row.ClientesFinaisAtivos,
            TotalAplicacoesAtivas:            row.AplicacoesAtivas,
            TotalLicencasAtivas:              row.LicencasAtivas,
            TotalLicencasInativas:            row.LicencasInativas,
            LicencasPorTipo: new LicencasPorTipoResult(
                row.Permanente, row.PorPeriodo, row.PorUsuarios, row.PorInstalacao),
            LicencasExpirandoEm7Dias:         row.LicencasExpirandoEm7Dias,
            SessoesAtivasAgora:               row.SessoesAtivasAgora,
            TokensExpirandoEm7Dias:           row.TokensExpirandoEm7Dias,
            NovasLicencasUltimos30Dias:        row.NovasLicencasUltimos30Dias,
            NovosClientesFinaisUltimos30Dias:  row.NovosClientesFinaisUltimos30Dias);
    }

    public async Task<DashboardAlertasResult> BuscarAlertasAsync(
        Guid idCliente, CancellationToken ct = default)
    {
        using var conn = factory.CreateConnection();

        // Query 1: Sessões inativas prolongadas (> 2x TempoLimiteSessaoHoras)
        const string sqlSessoes = """
            SELECT
                l.id                    AS "IdLicenca",
                ls.id                   AS "IdSessao",
                cf.razao_social         AS "ClienteFinalRazaoSocial",
                a.titulo                AS "AplicativoTitulo",
                ls.identificador_usuario AS "IdentificadorUsuario",
                ls.data_ultima_atividade AS "DataUltimaAtividade",
                EXTRACT(EPOCH FROM (NOW() - ls.data_ultima_atividade)) / 3600 AS "HorasInativa"
            FROM licenca_sessao ls
            JOIN licenca l  ON l.id  = ls.licenca_id
            JOIN licenca_usuarios lu ON lu.licenca_id = l.id
            JOIN cliente_final cf ON cf.id = l.id_cliente_final
            JOIN aplicacao a ON a.id = l.id_aplicativo
            WHERE l.id_cliente = @IdCliente
              AND ls.ativo = TRUE
              AND ls.data_ultima_atividade < NOW() - (lu.tempo_limite_sessao_horas * 2 * INTERVAL '1 hour')
            ORDER BY ls.data_ultima_atividade
            LIMIT 50
            """;

        // Query 2: Instalações adormecidas (> 30 dias sem validação)
        const string sqlInstalacoes = """
            SELECT
                l.id                         AS "IdLicenca",
                lir.id                       AS "IdInstalacao",
                cf.razao_social              AS "ClienteFinalRazaoSocial",
                a.titulo                     AS "AplicativoTitulo",
                lir.identificador_maquina    AS "IdentificadorMaquina",
                lir.data_ultima_validacao    AS "DataUltimaValidacao",
                EXTRACT(DAY FROM (NOW() - COALESCE(lir.data_ultima_validacao, lir.data_registro))) AS "DiasAdormecida"
            FROM licenca_instalacao_registrada lir
            JOIN licenca l  ON l.id  = lir.licenca_id
            JOIN cliente_final cf ON cf.id = l.id_cliente_final
            JOIN aplicacao a ON a.id = l.id_aplicativo
            WHERE l.id_cliente = @IdCliente
              AND lir.ativo = TRUE
              AND COALESCE(lir.data_ultima_validacao, lir.data_registro) < NOW() - INTERVAL '30 days'
            ORDER BY lir.data_ultima_validacao NULLS FIRST
            LIMIT 50
            """;

        // Query 3: Licenças no limite de capacidade
        const string sqlLimite = """
            SELECT u.idlicenca, u.razaosocial, u.tituloaplic, u.tipo, u.usoatual, u.maximo
            FROM (
                -- Licenças Por Usuários no limite
                SELECT
                    l.id AS idlicenca,
                    cf.razao_social AS razaosocial,
                    a.titulo AS tituloaplic,
                    'Por Usuários' AS tipo,
                    COUNT(ls.id) FILTER (WHERE ls.ativo = TRUE) AS usoatual,
                    lu.quantidade_maxima AS maximo
                FROM licenca l
                JOIN licenca_usuarios lu ON lu.licenca_id = l.id
                JOIN cliente_final cf ON cf.id = l.id_cliente_final
                JOIN aplicacao a ON a.id = l.id_aplicativo
                LEFT JOIN licenca_sessao ls ON ls.licenca_id = l.id
                WHERE l.id_cliente = @IdCliente AND l.ativo = TRUE
                GROUP BY l.id, cf.razao_social, a.titulo, lu.quantidade_maxima
                HAVING COUNT(ls.id) FILTER (WHERE ls.ativo = TRUE) >= lu.quantidade_maxima

                UNION ALL

                -- Licenças Por Instalação no limite
                SELECT
                    l.id,
                    cf.razao_social,
                    a.titulo,
                    'Por Instalação' AS tipo,
                    COUNT(lir.id) FILTER (WHERE lir.ativo = TRUE) AS usoatual,
                    li.quantidade_maxima AS maximo
                FROM licenca l
                JOIN licenca_instalacao li ON li.licenca_id = l.id
                JOIN cliente_final cf ON cf.id = l.id_cliente_final
                JOIN aplicacao a ON a.id = l.id_aplicativo
                LEFT JOIN licenca_instalacao_registrada lir ON lir.licenca_id = l.id
                WHERE l.id_cliente = @IdCliente AND l.ativo = TRUE
                GROUP BY l.id, cf.razao_social, a.titulo, li.quantidade_maxima
                HAVING COUNT(lir.id) FILTER (WHERE lir.ativo = TRUE) >= li.quantidade_maxima
            ) u
            ORDER BY u.usoatual DESC
            LIMIT 20
            """;

        // Query 4: Erros de validação nas últimas 24h
        const string sqlErros = """
            SELECT
                COUNT(*) FILTER (WHERE resultado = 'erro') AS "TotalErros",
                motivo_erro AS "Motivo",
                COUNT(*) AS "Total"
            FROM validacao_log
            WHERE id_licenca IN (SELECT id FROM licenca WHERE id_cliente = @IdCliente)
              AND resultado = 'erro'
              AND criado_em >= NOW() - INTERVAL '24 hours'
            GROUP BY motivo_erro
            ORDER BY COUNT(*) DESC
            """;

        const string sqlTopLicencasErros = """
            SELECT
                l.id                AS "IdLicenca",
                cf.razao_social     AS "ClienteFinalRazaoSocial",
                a.titulo            AS "AplicativoTitulo",
                COUNT(vl.id)        AS "TotalErros"
            FROM validacao_log vl
            JOIN licenca l  ON l.id = vl.id_licenca
            JOIN cliente_final cf ON cf.id = l.id_cliente_final
            JOIN aplicacao a ON a.id = l.id_aplicativo
            WHERE l.id_cliente = @IdCliente
              AND vl.resultado = 'erro'
              AND vl.criado_em >= NOW() - INTERVAL '24 hours'
            GROUP BY l.id, cf.razao_social, a.titulo
            ORDER BY COUNT(vl.id) DESC
            LIMIT 5
            """;

        var param = new { IdCliente = idCliente };

        var sessoesTask     = conn.QueryAsync<SessaoInativaAlerta>(new CommandDefinition(sqlSessoes, param, cancellationToken: ct));
        var instalacoesTask = conn.QueryAsync<InstalacaoAdormentaAlerta>(new CommandDefinition(sqlInstalacoes, param, cancellationToken: ct));
        var limiteTask      = conn.QueryAsync<LimitRaw>(new CommandDefinition(sqlLimite, param, cancellationToken: ct));
        var errosTask       = conn.QueryAsync<ErroRaw>(new CommandDefinition(sqlErros, param, cancellationToken: ct));
        var topErrosTask    = conn.QueryAsync<LicencaComMaisErros>(new CommandDefinition(sqlTopLicencasErros, param, cancellationToken: ct));

        await Task.WhenAll(sessoesTask, instalacoesTask, limiteTask, errosTask, topErrosTask);

        var errosRows = (await errosTask).AsList();
        var totalErros = errosRows.Sum(e => e.Total);
        var porMotivo = errosRows
            .Select(e => new ErrosPorMotivo(e.Motivo ?? "desconhecido", e.Total))
            .ToList();

        var limiteRows = (await limiteTask).AsList();
        var licencasLimite = limiteRows
            .Select(r => new LicencaLimiteAlerta(r.Idlicenca, r.Razaosocial, r.Tituloaplic, r.Tipo, r.Usoatual, r.Maximo))
            .ToList();

        return new DashboardAlertasResult(
            SessoesInativas:        (await sessoesTask).AsList(),
            InstalacoesAdormecidas: (await instalacoesTask).AsList(),
            LicencasNoLimite:       licencasLimite,
            ErrosValidacao: new ErrosValidacaoAlerta(
                TotalErros:            totalErros,
                PorMotivo:             porMotivo,
                LicencasComMaisErros:  (await topErrosTask).AsList()));
    }

    // ── Tipos de mapeamento internos ──────────────────────────────────────────

    private sealed record ResumoRaw(
        int ClientesFinaisAtivos,
        int AplicacoesAtivas,
        int LicencasAtivas,
        int LicencasInativas,
        int Permanente,
        int PorPeriodo,
        int PorUsuarios,
        int PorInstalacao,
        int LicencasExpirandoEm7Dias,
        int SessoesAtivasAgora,
        int TokensExpirandoEm7Dias,
        int NovasLicencasUltimos30Dias,
        int NovosClientesFinaisUltimos30Dias);

    private sealed record LimitRaw(
        Guid Idlicenca,
        string Razaosocial,
        string Tituloaplic,
        string Tipo,
        int Usoatual,
        int Maximo);

    private sealed record ErroRaw(string? Motivo, int Total);
}
