using Dapper;
using LicenciamentoSoftware.Application.Common;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Infrastructure.Persistence.Repositories;

public sealed class LicencaGestaoRepository(DbConnectionFactory factory) : ILicencaGestaoRepository
{
    private readonly DbConnectionFactory _factory = factory;

    public async Task<LicencaResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                l.id                        AS "Id",
                l.id_cliente                AS "IdCliente",
                l.id_cliente_final          AS "IdClienteFinal",
                cf.razao_social             AS "ClienteFinalRazaoSocial",
                l.id_aplicativo             AS "IdAplicativo",
                a.titulo                    AS "AplicativoTitulo",
                a.id_tipo_licenca           AS "IdTipoLicenca",
                tl.descricao                AS "TipoLicencaDescricao",
                l.data_cadastro             AS "DataCadastro",
                l.ativo                     AS "Ativo",
                lp.data_inicio              AS "Periodo_DataInicio",
                lp.data_fim                 AS "Periodo_DataFim",
                lp.renovacao_automatica     AS "Periodo_RenovacaoAutomatica",
                lu.quantidade_maxima        AS "Usuarios_QuantidadeMaxima",
                lu.max_sessoes_por_usuario  AS "Usuarios_MaxSessoesPorUsuario",
                lu.tempo_limite_sessao_horas AS "Usuarios_TempoLimiteSessaoHoras",
                li.quantidade_maxima        AS "Instalacao_QuantidadeMaxima",
                lt.id                       AS "Token_Id",
                (lt.criado_em + (lt.expiracao_minutos * INTERVAL '1 minute')) AS "Token_Expiracao",
                lt.ativo                    AS "Token_Ativo"
            FROM licenca l
            JOIN cliente_final cf ON cf.id = l.id_cliente_final
            JOIN aplicacao a ON a.id = l.id_aplicativo
            JOIN tipo_licenca tl ON tl.id = a.id_tipo_licenca
            LEFT JOIN licenca_periodo lp ON lp.licenca_id = l.id
            LEFT JOIN licenca_usuarios lu ON lu.licenca_id = l.id
            LEFT JOIN licenca_instalacao li ON li.licenca_id = l.id
            LEFT JOIN licenca_token lt ON lt.id_licenca = l.id AND lt.ativo = TRUE
            WHERE l.id = @Id
            LIMIT 1
            """;

        const string sqlSessoes = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_usuario   AS "IdentificadorUsuario",
                   data_login              AS "DataLogin",
                   data_ultima_atividade   AS "DataUltimaAtividade",
                   ativo                   AS "Ativo"
            FROM licenca_sessao
            WHERE licenca_id = @Id
            ORDER BY data_login DESC
            """;

        const string sqlInstalacoes = """
            SELECT id                      AS "Id",
                   licenca_id              AS "LicencaId",
                   identificador_maquina   AS "IdentificadorMaquina",
                   data_registro           AS "DataRegistro",
                   ativo                   AS "Ativo"
            FROM licenca_instalacao_registrada
            WHERE licenca_id = @Id
            ORDER BY data_registro DESC
            """;

        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        var licenca = rows.Select(MapRow).FirstOrDefault();
        if (licenca is null) return null;

        var sessoes = (await conn.QueryAsync<SessaoResult>(
            new CommandDefinition(sqlSessoes, new { Id = id }, cancellationToken: ct))).AsList();

        var instalacoes = (await conn.QueryAsync<InstalacaoRegistradaResult>(
            new CommandDefinition(sqlInstalacoes, new { Id = id }, cancellationToken: ct))).AsList();

        return licenca with { Sessoes = sessoes, InstalacoesRegistradas = instalacoes };
    }

    public async Task<bool> ExisteLicencaAtivaAsync(
        Guid idCliente, Guid idClienteFinal, Guid idAplicativo,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM licenca
                WHERE id_cliente       = @IdCliente
                  AND id_cliente_final = @IdClienteFinal
                  AND id_aplicativo    = @IdAplicativo
                  AND ativo            = TRUE
            )
            """;
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql,
                new { IdCliente = idCliente, IdClienteFinal = idClienteFinal, IdAplicativo = idAplicativo },
                cancellationToken: ct));
    }

    public async Task<PagedResult<LicencaResult>> ListarAsync(
        Guid? idCliente, Guid? idClienteFinal, Guid? idAplicativo,
        bool? ativo, int pagina, int tamanhoPagina,
        CancellationToken ct = default)
    {
        const string sqlCount = """
            SELECT COUNT(*) FROM licenca l
            WHERE (@IdCliente IS NULL OR l.id_cliente = @IdCliente)
              AND (@IdClienteFinal IS NULL OR l.id_cliente_final = @IdClienteFinal)
              AND (@IdAplicativo IS NULL OR l.id_aplicativo = @IdAplicativo)
              AND (@Ativo IS NULL OR l.ativo = @Ativo)
            """;

        const string sqlItens = """
            SELECT
                l.id                        AS "Id",
                l.id_cliente                AS "IdCliente",
                l.id_cliente_final          AS "IdClienteFinal",
                cf.razao_social             AS "ClienteFinalRazaoSocial",
                l.id_aplicativo             AS "IdAplicativo",
                a.titulo                    AS "AplicativoTitulo",
                a.id_tipo_licenca           AS "IdTipoLicenca",
                tl.descricao                AS "TipoLicencaDescricao",
                l.data_cadastro             AS "DataCadastro",
                l.ativo                     AS "Ativo",
                lp.data_inicio              AS "Periodo_DataInicio",
                lp.data_fim                 AS "Periodo_DataFim",
                lp.renovacao_automatica     AS "Periodo_RenovacaoAutomatica",
                lu.quantidade_maxima        AS "Usuarios_QuantidadeMaxima",
                lu.max_sessoes_por_usuario  AS "Usuarios_MaxSessoesPorUsuario",
                lu.tempo_limite_sessao_horas AS "Usuarios_TempoLimiteSessaoHoras",
                li.quantidade_maxima        AS "Instalacao_QuantidadeMaxima",
                (SELECT COUNT(*) FROM licenca_instalacao_registrada lir
                 WHERE lir.licenca_id = l.id AND lir.ativo = TRUE) AS "Instalacao_TotalAtivas",
                (SELECT COUNT(*) FROM licenca_sessao ls
                 WHERE ls.licenca_id = l.id AND ls.ativo = TRUE)   AS "Sessoes_TotalAtivas"
            FROM licenca l
            JOIN cliente_final cf ON cf.id = l.id_cliente_final
            JOIN aplicacao a ON a.id = l.id_aplicativo
            JOIN tipo_licenca tl ON tl.id = a.id_tipo_licenca
            LEFT JOIN licenca_periodo lp ON lp.licenca_id = l.id
            LEFT JOIN licenca_usuarios lu ON lu.licenca_id = l.id
            LEFT JOIN licenca_instalacao li ON li.licenca_id = l.id
            WHERE (@IdCliente IS NULL OR l.id_cliente = @IdCliente)
              AND (@IdClienteFinal IS NULL OR l.id_cliente_final = @IdClienteFinal)
              AND (@IdAplicativo IS NULL OR l.id_aplicativo = @IdAplicativo)
              AND (@Ativo IS NULL OR l.ativo = @Ativo)
            ORDER BY l.data_cadastro DESC
            LIMIT @Limite OFFSET @Offset
            """;

        var param = new
        {
            IdCliente = idCliente, IdClienteFinal = idClienteFinal,
            IdAplicativo = idAplicativo, Ativo = ativo,
            Limite = tamanhoPagina, Offset = (pagina - 1) * tamanhoPagina,
        };

        using var conn = _factory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, param, cancellationToken: ct));
        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(sqlItens, param, cancellationToken: ct));
        var itens = rows.Select(MapRow).ToList();

        return new PagedResult<LicencaResult>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Guid> InserirLicencaAsync(
        Domain.Entities.Licenca licenca, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO licenca (id, id_cliente, id_cliente_final, id_aplicativo, data_cadastro, ativo)
            VALUES (@Id, @IdCliente, @IdClienteFinal, @IdAplicativo, @DataCadastro, TRUE)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { licenca.Id, IdCliente = licenca.IdCliente,
                  IdClienteFinal = licenca.IdClienteFinal, IdAplicativo = licenca.IdAplicativo,
                  DataCadastro = licenca.DataCadastro },
            cancellationToken: ct));
        return licenca.Id;
    }

    public async Task InserirDetalhePeriodoAsync(
        Domain.Entities.LicencaPeriodo periodo, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO licenca_periodo (id, licenca_id, data_inicio, data_fim, renovacao_automatica)
            VALUES (@Id, @LicencaId, @DataInicio, @DataFim, @RenovacaoAutomatica)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { periodo.Id, LicencaId = periodo.LicencaId,
                  DataInicio = periodo.DataInicio, DataFim = periodo.DataFim,
                  RenovacaoAutomatica = periodo.RenovacaoAutomatica },
            cancellationToken: ct));
    }

    public async Task InserirDetalheUsuariosAsync(
        Domain.Entities.LicencaUsuarios usuarios, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO licenca_usuarios
                (id, licenca_id, quantidade_maxima, max_sessoes_por_usuario, tempo_limite_sessao_horas)
            VALUES
                (@Id, @LicencaId, @QuantidadeMaxima, @MaxSessoesPorUsuario, @TempoLimiteSessaoHoras)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { usuarios.Id, LicencaId = usuarios.LicencaId,
                  QuantidadeMaxima = usuarios.QuantidadeMaxima,
                  MaxSessoesPorUsuario = usuarios.MaxSessoesPorUsuario,
                  TempoLimiteSessaoHoras = usuarios.TempoLimiteSessaoHoras },
            cancellationToken: ct));
    }

    public async Task InserirDetalheInstalacaoAsync(
        Domain.Entities.LicencaInstalacao instalacao, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO licenca_instalacao (id, licenca_id, quantidade_maxima)
            VALUES (@Id, @LicencaId, @QuantidadeMaxima)
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { instalacao.Id, LicencaId = instalacao.LicencaId,
                  QuantidadeMaxima = instalacao.QuantidadeMaxima },
            cancellationToken: ct));
    }

    public async Task DesativarAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "UPDATE licenca SET ativo = FALSE WHERE id = @Id";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<DetalhePeriodoResult?> BuscarPeriodoPorLicencaAsync(
        Guid idLicenca, CancellationToken ct = default)
    {
        const string sql = """
            SELECT data_inicio          AS "DataInicio",
                   data_fim             AS "DataFim",
                   renovacao_automatica AS "RenovacaoAutomatica"
            FROM licenca_periodo
            WHERE licenca_id = @IdLicenca
            LIMIT 1
            """;
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DetalhePeriodoResult>(
            new CommandDefinition(sql, new { IdLicenca = idLicenca }, cancellationToken: ct));
    }

    public async Task AtualizarDataFimPeriodoAsync(
        Guid idLicenca, DateTime novaDataFim, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_periodo SET data_fim = @DataFim WHERE licenca_id = @IdLicenca
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { IdLicenca = idLicenca, DataFim = novaDataFim },
            cancellationToken: ct));
    }

    public async Task AtualizarDetalhesUsuariosAsync(
        Guid idLicenca, int quantidadeMaxima, int maxSessoesPorUsuario, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_usuarios
            SET quantidade_maxima = @QuantidadeMaxima,
                max_sessoes_por_usuario = @MaxSessoesPorUsuario
            WHERE licenca_id = @IdLicenca
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { IdLicenca = idLicenca, QuantidadeMaxima = quantidadeMaxima, MaxSessoesPorUsuario = maxSessoesPorUsuario },
            cancellationToken: ct));
    }

    public async Task AtualizarDetalhesInstalacaoAsync(
        Guid idLicenca, int quantidadeMaxima, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_instalacao
            SET quantidade_maxima = @QuantidadeMaxima
            WHERE licenca_id = @IdLicenca
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { IdLicenca = idLicenca, QuantidadeMaxima = quantidadeMaxima },
            cancellationToken: ct));
    }

    public async Task AtualizarRenovacaoAutomaticaAsync(
        Guid idLicenca, bool renovacaoAutomatica, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE licenca_periodo
            SET renovacao_automatica = @RenovacaoAutomatica
            WHERE licenca_id = @IdLicenca
            """;
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { IdLicenca = idLicenca, RenovacaoAutomatica = renovacaoAutomatica },
            cancellationToken: ct));
    }

    // -------------------------------------------------------------------------
    // Fase 8 — jobs de expiração, renovação automática e notificação
    // -------------------------------------------------------------------------

    public async Task<IReadOnlyList<Application.Jobs.LicencaPeriodoJobInfo>> BuscarLicencasPeriodoVencidasAsync(
        DateTime agora, CancellationToken ct = default)
    {
        const string sql = """
            SELECT l.id                  AS "IdLicenca",
                   l.id_cliente          AS "IdCliente",
                   a.titulo             AS "NomeAplicacao",
                   lp.data_inicio       AS "DataInicio",
                   lp.data_fim          AS "DataFim",
                   lp.renovacao_automatica AS "RenovacaoAutomatica"
            FROM licenca l
            JOIN aplicacao a       ON a.id = l.id_aplicativo
            JOIN licenca_periodo lp ON lp.licenca_id = l.id
            WHERE l.ativo                  = TRUE
              AND lp.renovacao_automatica  = FALSE
              AND lp.data_fim              < @Agora
            """;
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<Application.Jobs.LicencaPeriodoJobInfo>(
            new CommandDefinition(sql, new { Agora = agora }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Application.Jobs.LicencaPeriodoJobInfo>> BuscarLicencasRenovacaoAutomaticaAsync(
        DateTime agora, int diasAntecedencia, CancellationToken ct = default)
    {
        const string sql = """
            SELECT l.id                  AS "IdLicenca",
                   l.id_cliente          AS "IdCliente",
                   a.titulo             AS "NomeAplicacao",
                   lp.data_inicio       AS "DataInicio",
                   lp.data_fim          AS "DataFim",
                   lp.renovacao_automatica AS "RenovacaoAutomatica"
            FROM licenca l
            JOIN aplicacao a       ON a.id = l.id_aplicativo
            JOIN licenca_periodo lp ON lp.licenca_id = l.id
            WHERE l.ativo                 = TRUE
              AND lp.renovacao_automatica = TRUE
              AND lp.data_fim             <= (@Agora + (@Dias * INTERVAL '1 day'))
            """;
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<Application.Jobs.LicencaPeriodoJobInfo>(
            new CommandDefinition(sql, new { Agora = agora, Dias = diasAntecedencia },
                cancellationToken: ct));
        return rows.AsList();
    }

    public async Task DesativarLicencasPeriodoVencidasAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        const string sql = "UPDATE licenca SET ativo = FALSE WHERE id = ANY(@Ids)";
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Ids = ids.ToArray() }, cancellationToken: ct));
    }

    public Task RenovarDataFimLicencaAsync(
        Guid idLicenca, DateTime novaDataFim, CancellationToken ct = default)
        => AtualizarDataFimPeriodoAsync(idLicenca, novaDataFim, ct);

    public async Task<IReadOnlyList<Application.Jobs.LicencaPeriodoJobInfo>> BuscarLicencasProximasVencimentoAsync(
        DateTime agora, int diasAntecedencia, CancellationToken ct = default)
    {
        const string sql = """
            SELECT l.id                  AS "IdLicenca",
                   l.id_cliente          AS "IdCliente",
                   a.titulo             AS "NomeAplicacao",
                   lp.data_inicio       AS "DataInicio",
                   lp.data_fim          AS "DataFim",
                   lp.renovacao_automatica AS "RenovacaoAutomatica"
            FROM licenca l
            JOIN aplicacao a       ON a.id = l.id_aplicativo
            JOIN licenca_periodo lp ON lp.licenca_id = l.id
            WHERE l.ativo    = TRUE
              AND lp.data_fim > @Agora
              AND lp.data_fim <= (@Agora + (@Dias * INTERVAL '1 day'))
            ORDER BY lp.data_fim
            """;
        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<Application.Jobs.LicencaPeriodoJobInfo>(
            new CommandDefinition(sql, new { Agora = agora, Dias = diasAntecedencia },
                cancellationToken: ct));
        return rows.AsList();
    }

    // -------------------------------------------------------------------------
    // Mapeamento de linha dinâmica para LicencaResult
    // -------------------------------------------------------------------------
    private static LicencaResult MapRow(dynamic r)
    {
        DetalhePeriodoResult? periodo = null;
        if (r.Periodo_DataInicio is not null)
        {
            periodo = new DetalhePeriodoResult(
                (DateTime)r.Periodo_DataInicio,
                (DateTime)r.Periodo_DataFim,
                (bool)r.Periodo_RenovacaoAutomatica);
        }

        DetalheUsuariosResult? usuarios = null;
        if (r.Usuarios_QuantidadeMaxima is not null)
        {
            usuarios = new DetalheUsuariosResult(
                (int)r.Usuarios_QuantidadeMaxima,
                (int)r.Usuarios_MaxSessoesPorUsuario,
                (int)r.Usuarios_TempoLimiteSessaoHoras);
        }

        DetalheInstalacaoResult? instalacao = null;
        if (r.Instalacao_QuantidadeMaxima is not null)
        {
            instalacao = new DetalheInstalacaoResult(
                (int)r.Instalacao_QuantidadeMaxima);
        }

        return new LicencaResult(
            (Guid)r.Id,
            (Guid)r.IdCliente,
            (Guid)r.IdClienteFinal,
            (string)(r.ClienteFinalRazaoSocial ?? string.Empty),
            (Guid)r.IdAplicativo,
            (string)(r.AplicativoTitulo ?? string.Empty),
            (Guid)r.IdTipoLicenca,
            (string)r.TipoLicencaDescricao,
            (DateTime)r.DataCadastro,
            (bool)r.Ativo,
            periodo,
            usuarios,
            instalacao,
            // Popula sessões com objetos placeholder para exibir a contagem no card
            Sessoes: usuarios is not null && r.Sessoes_TotalAtivas is not null
                ? Enumerable.Range(0, (int)(long)r.Sessoes_TotalAtivas)
                    .Select(_ => new SessaoResult(Guid.Empty, Guid.Empty, string.Empty, DateTime.MinValue, DateTime.MinValue, true))
                    .ToList()
                : null,
            // Popula instalações com objetos placeholder apenas para exibir a contagem no card
            InstalacoesRegistradas: instalacao is not null && r.Instalacao_TotalAtivas is not null
                ? Enumerable.Range(0, (int)(long)r.Instalacao_TotalAtivas)
                    .Select(_ => new InstalacaoRegistradaResult(Guid.Empty, Guid.Empty, string.Empty, DateTime.MinValue, true))
                    .ToList()
                : null,
            Token: r.Token_Id is null ? null
                : new TokenInfoResult((Guid)r.Token_Id, (DateTime)r.Token_Expiracao, (bool)r.Token_Ativo));
    }
}
