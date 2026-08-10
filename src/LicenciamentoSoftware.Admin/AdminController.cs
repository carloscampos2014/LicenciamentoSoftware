using System.Diagnostics;
using System.Globalization;
using System.Text;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;

namespace LicenciamentoSoftware.Admin;

/// <summary>
/// Handlers dos endpoints do painel Admin.
/// Página HTML gerada em C# — sem Razor, sem Blazor.
/// </summary>
public static class AdminController
{
    // ─────────────────────────────────────────────────────────────────────────
    // GET / — página principal do painel
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> Index(
        AdminMetricasRepository repo,
        IHttpClientFactory httpFactory,
        IConfiguration config)
    {
        // Busca em paralelo para minimizar latência
        var metricasTask    = repo.BuscarMetricasGeraisAsync();
        var errosTask       = repo.BuscarErrosPorMotivoAsync();
        var ultimosLogsTask = repo.BuscarUltimosLoginsAsync();
        var porTipoTask     = repo.BuscarLicencasPorTipoAsync();
        var tamanhoBdTask   = repo.BuscarTamanhoBancoAsync();
        var pendentesTask   = repo.ContarReset2FAPendentesAsync();

        await Task.WhenAll(metricasTask, errosTask, ultimosLogsTask, porTipoTask, tamanhoBdTask, pendentesTask);

        var metricas    = metricasTask.Result;
        var erros       = errosTask.Result;
        var ultimosLogs = ultimosLogsTask.Result;
        var porTipo     = porTipoTask.Result;
        var tamanhoBd   = tamanhoBdTask.Result;
        var pendentes2FA = pendentesTask.Result;

        // Status dos serviços
        var client = httpFactory.CreateClient("health");
        var apiUp  = await PingAsync(client, config["AdminSettings:ApiHealthUrl"] ?? "http://localhost:5016/health");
        var bffUp  = await PingAsync(client, config["AdminSettings:BffHealthUrl"] ?? "http://localhost:5017/health");

        // Status do backup
        var backupDir    = config["AdminSettings:BackupDir"] ?? "/opt/backups";
        var backupStatus = LerStatusBackup(backupDir);

        var agora = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

        var html = GerarHtml(metricas, erros, ultimosLogs, porTipo, tamanhoBd,
                             apiUp, bffUp, backupStatus, agora, pendentes2FA);

        return Results.Content(html, "text/html; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /backup/executar — dispara pg_dump manualmente
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> ExecutarBackup(IConfiguration config, ILogger<Program> logger)
    {
        var script = config["AdminSettings:BackupScript"] ?? "/opt/scripts/backup-db.sh";

        if (!File.Exists(script))
            return Results.Problem($"Script de backup não encontrado: {script}", statusCode: 500);

        try
        {
            var psi = new ProcessStartInfo("/bin/bash", script)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Não foi possível iniciar o processo.");

            var saida = await proc.StandardOutput.ReadToEndAsync();
            var erro  = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
            {
                _logBackupFalhou(logger, proc.ExitCode, erro, null);
                return Results.Problem($"Backup falhou (exit {proc.ExitCode}): {erro}", statusCode: 500);
            }

            _logBackupSucesso(logger, null);
            return Results.Ok(new { Mensagem = "Backup executado com sucesso.", Saida = saida });
        }
        catch (Exception ex)
        {
            _logBackupErro(logger, ex);
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static readonly Action<ILogger, int, string, Exception?> _logBackupFalhou =
        LoggerMessage.Define<int, string>(LogLevel.Error, new EventId(1, "BackupFalhou"),
            "Backup manual falhou. Exit={ExitCode} Erro={Erro}");

    private static readonly Action<ILogger, Exception?> _logBackupSucesso =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "BackupSucesso"),
            "Backup manual executado com sucesso.");

    private static readonly Action<ILogger, Exception?> _logBackupErro =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, "BackupErro"),
            "Erro ao executar backup manual.");

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<bool> PingAsync(HttpClient client, string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Passa o Host correto para evitar rejeição pelo AllowedHosts do ASP.NET Core
            var uri = new Uri(url);
            request.Headers.Host = uri.Host + (uri.IsDefaultPort ? "" : $":{uri.Port}");
            var resp = await client.SendAsync(request);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static BackupStatus LerStatusBackup(string backupDir)
    {
        try
        {
            if (!Directory.Exists(backupDir))
                return new BackupStatus(null, null, "Diretório não encontrado", false);

            var arquivos = Directory.GetFiles(backupDir, "*.sql.gz")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToList();

            if (arquivos.Count == 0)
                return new BackupStatus(null, null, "Nenhum backup encontrado", false);

            var ultimo    = arquivos[0];
            var dataHora  = File.GetLastWriteTimeUtc(ultimo);
            var tamanho   = new FileInfo(ultimo).Length;
            var ok        = dataHora >= DateTime.UtcNow.AddHours(-25); // margem de 1h
            var tamanhoFmt = tamanho < 1024 * 1024
                ? $"{tamanho / 1024.0:F1} KB"
                : $"{tamanho / 1024.0 / 1024.0:F1} MB";

            return new BackupStatus(
                dataHora.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " UTC",
                tamanhoFmt,
                Path.GetFileName(ultimo),
                ok);
        }
        catch (Exception ex)
        {
            return new BackupStatus(null, null, $"Erro: {ex.Message}", false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Geração do HTML
    // ─────────────────────────────────────────────────────────────────────────

    private static string GerarHtml(
        MetricasGerais m, IReadOnlyList<ErroMotivo> erros,
        IReadOnlyList<UltimoLogin> logins, IReadOnlyList<LicencaPorTipo> porTipo,
        string tamanhoBd, bool apiUp, bool bffUp, BackupStatus backup, string agora,
        int pendentes2FA = 0)
    {
        var sb  = new StringBuilder();
        var ic  = CultureInfo.InvariantCulture;

        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>Admin — LicenciamentoSoftware</title>
              <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css">
              <style>
                body { background:#f8f9fa; }
                .metric-card { border-left: 4px solid #6c63ff; }
                .metric-val  { font-size: 2rem; font-weight: 700; color: #6c63ff; }
                .ok  { color: #198754; font-weight:600; }
                .err { color: #dc3545; font-weight:600; }
                .warn{ color: #fd7e14; font-weight:600; }
                .badge-tipo { font-size:.75rem; }
                thead th { background:#6c63ff; color:#fff; }
                #refresh-btn { cursor:pointer; font-size:.8rem; padding:2px 10px; }
                #countdown { font-variant-numeric: tabular-nums; min-width:2ch; display:inline-block; }
              </style>
            </head>
            <body>
            <nav class="navbar navbar-dark" style="background:#6c63ff">
              <div class="container-fluid">
                <span class="navbar-brand fw-bold">🔧 LicenciamentoSoftware — Painel Admin</span>
                <div class="d-flex align-items-center gap-3">
                  <span class="text-white-50 small">
                    Atualizado: 
            """);

        sb.Append(agora);

        sb.AppendLine("""
                  &nbsp;|&nbsp; refresh em <span id="countdown">30</span>s
                  </span>
                  <button id="refresh-btn" class="btn btn-sm btn-outline-light" onclick="location.reload()">
                    ↻ Atualizar agora
                  </button>
                </div>
              </div>
            </nav>
            <script>
              (function() {
                var s = 30;
                var el = document.getElementById('countdown');
                var iv = setInterval(function() {
                  s--;
                  if (el) el.textContent = s;
                  if (s <= 0) { clearInterval(iv); location.reload(); }
                }, 1000);
              })();
            </script>
            <div class="container-fluid py-4">
            """);

        // ── Alerta de reset 2FA pendentes ─────────────────────────────────────
        if (pendentes2FA > 0)
        {
            sb.AppendLine(ic, $"""
                <div class='alert alert-warning d-flex align-items-center justify-content-between mb-4 shadow-sm' role='alert'>
                  <div>
                    <strong>⚠️ {pendentes2FA} solicitaç{(pendentes2FA > 1 ? "ões" : "ão")} de reset de 2FA pendente{(pendentes2FA > 1 ? "s" : "")}</strong>
                    <span class='ms-2 text-muted small'>Aguardando aprovação do administrador</span>
                  </div>
                  <a href='/reset-2fa/pendentes' class='btn btn-warning btn-sm fw-bold ms-3' style='white-space:nowrap'>
                    🔑 Ver solicitações →
                  </a>
                </div>
                """);
        }
        else
        {
            sb.AppendLine("""
                <div class='mb-3 text-end'>
                  <a href='/reset-2fa/pendentes' class='text-muted small text-decoration-none'>🔑 Solicitações de reset 2FA</a>
                </div>
                """);
        }

        // ── Métricas gerais ───────────────────────────────────────────────────
        sb.AppendLine("<h5 class='text-muted mb-3'>Visão Geral</h5>");
        sb.AppendLine("<div class='row g-3 mb-4'>");

        void Card(string titulo, string valor, string? sub = null) =>
            sb.AppendLine(ic, $"""
                <div class='col-6 col-md-3 col-xl-2'>
                  <div class='card metric-card shadow-sm h-100'>
                    <div class='card-body py-3'>
                      <div class='text-muted small'>{titulo}</div>
                      <div class='metric-val'>{valor}</div>
                      {(sub is not null ? $"<div class='text-muted' style='font-size:.75rem'>{sub}</div>" : "")}
                    </div>
                  </div>
                </div>
                """);

        Card("Clientes Ativos",      m.ClientesAtivos.ToString(ic),           $"Total: {m.TotalClientes.ToString(ic)} | Encerrados: {m.ClientesEncerrados.ToString(ic)}");
        Card("Usuários Ativos",      m.UsuariosAtivos.ToString(ic));
        Card("Licenças Ativas",      m.LicencasAtivas.ToString(ic),            $"Inativas: {m.LicencasInativas.ToString(ic)}");
        Card("Expirando em 7 dias",  m.LicencasExpirandoEm7Dias.ToString(ic),  m.LicencasExpirandoEm7Dias > 0 ? "⚠️ atenção" : "✅ ok");
        Card("Sessões Abertas",      m.SessoesAbertas.ToString(ic));
        Card("Validações 24h",       m.ValidacoesUltimas24h.ToString(ic),      $"7 dias: {m.ValidacoesUltimos7Dias.ToString("N0", ic)}");
        Card("Erros 24h",            m.ErrosUltimas24h.ToString(ic),           m.ErrosUltimas24h > 0 ? "⚠️ ver abaixo" : "✅ nenhum");
        Card("Tamanho do banco",     tamanhoBd);

        sb.AppendLine("</div>");

        // ── Status dos serviços ───────────────────────────────────────────────
        sb.AppendLine("""
            <h5 class='text-muted mb-3'>Status dos Serviços</h5>
            <div class='row g-3 mb-4'>
            """);

        void Servico(string nome, bool up) =>
            sb.AppendLine(ic, $"""
                <div class='col-6 col-md-3'>
                  <div class='card shadow-sm'>
                    <div class='card-body py-3 text-center'>
                      <div class='small text-muted'>{nome}</div>
                      <div class='{(up ? "ok" : "err")}' style='font-size:1.4rem'>{(up ? "● Online" : "● Offline")}</div>
                    </div>
                  </div>
                </div>
                """);

        Servico("API (porta 5016)", apiUp);
        Servico("BFF (porta 5017)", bffUp);
        sb.AppendLine("</div>");

        // ── Backup ────────────────────────────────────────────────────────────
        var bkClass = backup.Ok ? "ok" : "err";
        var bkIcon  = backup.Ok ? "✅" : "🔴";

        sb.AppendLine(ic, $"""
            <h5 class='text-muted mb-3'>Backup do Banco</h5>
            <div class='card shadow-sm mb-4'>
              <div class='card-body'>
                <div class='row align-items-center'>
                  <div class='col'>
                    <span class='{bkClass}'>{bkIcon} {(backup.Ok ? "Backup recente OK" : "Backup desatualizado ou ausente")}</span><br>
                    <span class='text-muted small'>Último: {backup.DataHora ?? "N/D"} &nbsp;|&nbsp; Arquivo: {backup.Arquivo ?? "N/D"} &nbsp;|&nbsp; Tamanho: {backup.Tamanho ?? "N/D"}</span>
                  </div>
                  <div class='col-auto'>
                    <form method='post' action='/backup/executar'>
                      <button type='submit' class='btn btn-outline-secondary btn-sm'>⚙️ Executar backup agora</button>
                    </form>
                  </div>
                </div>
              </div>
            </div>
            """);

        // ── Licenças por tipo ─────────────────────────────────────────────────
        if (porTipo.Count > 0)
        {
            sb.AppendLine("""
                <div class='row g-3 mb-4'>
                  <div class='col-md-4'>
                    <h5 class='text-muted mb-3'>Licenças por Tipo</h5>
                    <div class='card shadow-sm'>
                      <div class='card-body p-0'>
                        <table class='table table-sm mb-0'>
                          <thead><tr><th>Tipo</th><th class='text-end'>Total</th></tr></thead>
                          <tbody>
                """);
            foreach (var t in porTipo)
                sb.AppendLine(ic, $"<tr><td>{t.Tipo}</td><td class='text-end fw-bold'>{t.Total.ToString(ic)}</td></tr>");
            sb.AppendLine("</tbody></table></div></div></div>");

            // ── Erros por motivo ──────────────────────────────────────────────
            sb.AppendLine("""
                  <div class='col-md-4'>
                    <h5 class='text-muted mb-3'>Erros por Motivo (24h)</h5>
                    <div class='card shadow-sm'>
                      <div class='card-body p-0'>
                        <table class='table table-sm mb-0'>
                          <thead><tr><th>Motivo</th><th class='text-end'>Total</th></tr></thead>
                          <tbody>
                """);
            if (erros.Count == 0)
                sb.AppendLine("<tr><td colspan='2' class='text-center text-muted'>Nenhum erro ✅</td></tr>");
            else
                foreach (var e in erros)
                    sb.AppendLine(ic, $"<tr><td>{e.Motivo}</td><td class='text-end text-danger fw-bold'>{e.Total.ToString(ic)}</td></tr>");
            sb.AppendLine("</tbody></table></div></div></div></div>");
        }

        // ── Últimos logins ────────────────────────────────────────────────────
        if (logins.Count > 0)
        {
            sb.AppendLine("""
                <h5 class='text-muted mb-3'>Últimos Logins Válidos</h5>
                <div class='card shadow-sm mb-4'>
                  <div class='card-body p-0'>
                    <table class='table table-sm table-hover mb-0'>
                      <thead><tr><th>E-mail / Tenant</th><th>IP</th><th>Data/Hora (UTC)</th></tr></thead>
                      <tbody>
                """);
            foreach (var l in logins)
                sb.AppendLine(ic, $"<tr><td>{l.Email}</td><td><code>{l.Ip ?? "-"}</code></td><td>{l.HoraUtc.ToString("dd/MM/yyyy HH:mm:ss", ic)}</td></tr>");
            sb.AppendLine("</tbody></table></div></div>");
        }

        sb.AppendLine("""
            </div><!-- /container -->
            </body>
            </html>
            """);

        return sb.ToString();
    }

    private sealed record BackupStatus(string? DataHora, string? Tamanho, string? Arquivo, bool Ok);
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> ListarUsuarios(AdminMetricasRepository repo)
    {
        var usuarios = await repo.ListarUsuariosAsync();
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html><html lang="pt-BR"><head><meta charset="UTF-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Usuários — Painel Admin</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
            </head><body class="bg-light p-4">
            <div class="container-fluid">
            <h4 class="mb-3">👤 Usuários da Plataforma <a href="/" class="btn btn-sm btn-outline-secondary ms-3">← Voltar</a></h4>
            <table class="table table-sm table-bordered table-hover bg-white shadow-sm">
            <thead class="table-dark">
            <tr><th>Nome</th><th>E-mail</th><th>Empresa</th><th>Papel</th><th>Ativo</th><th>2FA</th><th>Ação</th></tr>
            </thead><tbody>
            """);

        foreach (var u in usuarios)
        {
            var ativo  = u.Ativo  ? "✅" : "❌";
            var totp   = u.TotpAtivo ? "✅ Ativo" : "⬜ Inativo";
            var btnReset = u.TotpAtivo
                ? $"""<button class="btn btn-sm btn-warning" onclick="resetar2fa('{u.Id}','{u.Nome}')">Resetar 2FA</button>"""
                : "<span class=\"text-muted small\">—</span>";

            sb.AppendLine(ic,
                $"<tr><td>{u.Nome}</td><td>{u.Email}</td><td>{u.NomeCliente}</td><td><code>{u.Papel ?? "—"}</code></td><td>{ativo}</td><td>{totp}</td><td>{btnReset}</td></tr>");
        }

        sb.AppendLine("""
            </tbody></table></div>
            <script>
            async function resetar2fa(id, nome) {
              if (!confirm(`Resetar o 2FA de "${nome}"?\n\nO usuário poderá fazer login sem 2FA e deverá configurar um novo autenticador.`)) return;
              const r = await fetch(`/usuarios/${id}/reset-2fa`, { method: 'POST' });
              if (r.ok) { alert('2FA resetado com sucesso!'); location.reload(); }
              else { alert('Erro ao resetar: ' + r.status); }
            }
            </script>
            </body></html>
            """);

        return Results.Content(sb.ToString(), "text/html; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /usuarios/{id}/reset-2fa — reseta o TOTP de um usuário
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> ResetarTotp(
        Guid id,
        ResetarTotpAdminHandler handler,
        ILogger<Program> logger)
    {
        var resultado = await handler.HandleAsync(id);

        return resultado switch
        {
            ResetarTotpAdminResult.Sucesso        => Results.NoContent(),
            ResetarTotpAdminResult.UsuarioNaoEncontrado => Results.NotFound(new { Erro = "Usuário não encontrado." }),
            _ => Results.Problem("Erro interno.", statusCode: 500),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /reset-2fa/pendentes — lista solicitações pendentes de reset de 2FA
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> ListarSolicitacoesPendentes(
        ISolicitacaoReset2FARepository repo)
    {
        var pendentes = await repo.ListarPendentesAsync();
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang='pt-BR'><head><meta charset='UTF-8'>");
        sb.AppendLine("<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css'>");
        sb.AppendLine("</head><body class='bg-light p-4'><div class='container-fluid'>");
        sb.AppendLine("<h4 class='mb-3'>Solicitacoes de Reset 2FA <a href='/' class='btn btn-sm btn-outline-secondary ms-3'>Voltar</a></h4>");
        if (!pendentes.Any())
        {
            sb.AppendLine("<div class='alert alert-success'>Nenhuma solicitacao pendente.</div>");
        }
        else
        {
            sb.AppendLine("<table class='table table-bordered table-hover bg-white shadow-sm'>");
            sb.AppendLine("<thead class='table-warning'><tr><th>Usuario</th><th>Email</th><th>Empresa</th><th>IP</th><th>Data/Hora</th><th>Acoes</th></tr></thead><tbody>");
            foreach (var s in pendentes)
            {
                var tr = string.Format(ic,
                    "<tr><td>{0}</td><td>{1}</td><td>{2}</td><td><code>{3}</code></td><td>{4} UTC</td>" +
                    "<td><button class='btn btn-sm btn-success me-1' onclick=\"processar('{5}','aprovar','{0}')\">Aprovar</button> " +
                    "<button class='btn btn-sm btn-danger' onclick=\"processar('{5}','rejeitar','{0}')\">Rejeitar</button></td></tr>",
                    s.NomeUsuario, s.EmailUsuario, s.NomeCliente,
                    s.IpOrigem ?? "-", s.CriadoEm.ToString("dd/MM/yyyy HH:mm:ss", ic), s.Id);
                sb.AppendLine(tr);
            }
            sb.AppendLine("</tbody></table>");
        }
        sb.AppendLine("<script>");
        sb.AppendLine("async function processar(id,acao,nome){ if(!confirm('Confirma '+acao+' para '+nome+'?'))return;");
        sb.AppendLine("const r=await fetch('/reset-2fa/'+id+'/'+acao,{method:'POST'});if(r.ok){alert('OK!');location.reload();}else{alert('Erro '+r.status);} }");
        sb.AppendLine("</script></div></body></html>");
        return Results.Content(sb.ToString(), "text/html; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /reset-2fa/{id}/aprovar — executa o reset aprovado
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> AprovarSolicitacaoReset(
        Guid id,
        AprovarReset2FAHandler handler)
    {
        var resultado = await handler.HandleAsync(id);
        return resultado switch
        {
            AprovarReset2FAResult.Sucesso            => Results.NoContent(),
            AprovarReset2FAResult.SolicitacaoNaoEncontrada => Results.NotFound(new { Erro = "Solicitação não encontrada." }),
            AprovarReset2FAResult.JaProcessada       => Results.Conflict(new { Erro = "Solicitação já foi processada." }),
            _ => Results.Problem("Erro interno.", statusCode: 500),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /reset-2fa/{id}/rejeitar — rejeita a solicitação
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<IResult> RejeitarSolicitacaoReset(
        Guid id,
        ISolicitacaoReset2FARepository repo,
        IUnitOfWork uow)
    {
        await uow.BeginAsync();
        try
        {
            await repo.RejeitarAsync(id);
            await uow.CommitAsync();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
        return Results.NoContent();
    }
}
