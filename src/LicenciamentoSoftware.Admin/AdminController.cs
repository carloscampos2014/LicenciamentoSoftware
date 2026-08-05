using System.Diagnostics;
using System.Globalization;
using System.Text;

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
        var metricas    = await repo.BuscarMetricasGeraisAsync();
        var erros       = await repo.BuscarErrosPorMotivoAsync();
        var ultimosLogs = await repo.BuscarUltimosLoginsAsync();
        var porTipo     = await repo.BuscarLicencasPorTipoAsync();
        var tamanhoBd   = await repo.BuscarTamanhoBancoAsync();

        // Status dos serviços
        var client = httpFactory.CreateClient("health");
        var apiUp  = await PingAsync(client, config["AdminSettings:ApiHealthUrl"] ?? "http://localhost:5016/health");
        var bffUp  = await PingAsync(client, config["AdminSettings:BffHealthUrl"] ?? "http://localhost:5017/health");

        // Status do backup
        var backupDir    = config["AdminSettings:BackupDir"] ?? "/opt/backups";
        var backupStatus = LerStatusBackup(backupDir);

        var agora = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

        var html = GerarHtml(metricas, erros, ultimosLogs, porTipo, tamanhoBd,
                             apiUp, bffUp, backupStatus, agora);

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
            var resp = await client.GetAsync(url);
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
        string tamanhoBd, bool apiUp, bool bffUp, BackupStatus backup, string agora)
    {
        var sb  = new StringBuilder();
        var ic  = CultureInfo.InvariantCulture;

        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta http-equiv="refresh" content="30">
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
              </style>
            </head>
            <body>
            <nav class="navbar navbar-dark" style="background:#6c63ff">
              <div class="container-fluid">
                <span class="navbar-brand fw-bold">🔧 LicenciamentoSoftware — Painel Admin</span>
                <span class="text-white-50 small">Atualizado: 
            """);

        sb.Append(agora);

        sb.AppendLine("""
                  &nbsp;|&nbsp; auto-refresh a cada 30s
                </span>
              </div>
            </nav>
            <div class="container-fluid py-4">
            """);

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
}
