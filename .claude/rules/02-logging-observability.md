---
description: "Logging estruturado obrigatório — níveis, rotação, expurgo automático, auditoria e notificações real-time."
---

# Logging e Observabilidade

Todo projeto deve ter logging estruturado desde o início. Observabilidade não é opcional.

## Níveis de Log

| Nível | Quando usar |
|-------|-------------|
| `DEBUG` | Valores intermediários, fluxo de execução (só em dev) |
| `INFO` | Operações concluídas com sucesso, eventos de negócio |
| `WARNING` | Situação inesperada mas recuperável; degradação de serviço |
| `ERROR` | Falha que impede a operação atual; requer atenção |
| `CRITICAL` | Sistema indisponível; requer ação imediata |

## FileLoggingService C# (sem dependências pagas)

```csharp
public interface ILoggingService
{
    void LogInfo(string message, object? data = null);
    void LogWarning(string message, object? data = null);
    void LogError(string message, Exception? ex = null, object? data = null);
    void LogAudit(string action, string userId, object? data = null);
}

public class FileLoggingService : ILoggingService, IDisposable
{
    private readonly string _logDirectory;
    private readonly Timer _purgeTimer;
    private const int AppLogRetentionDays = 30;
    private const int AuditLogRetentionDays = 90;

    public FileLoggingService(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
        // Purge diário às 2h
        _purgeTimer = new Timer(PurgeLogs, null,
            TimeSpan.FromHours(2), TimeSpan.FromHours(24));
    }

    public void LogInfo(string message, object? data = null) =>
        WriteLog("INFO", message, data: data);

    public void LogError(string message, Exception? ex = null, object? data = null) =>
        WriteLog("ERROR", message, ex, data);

    public void LogAudit(string action, string userId, object? data = null) =>
        WriteLog("AUDIT", action, data: data, userId: userId, isAudit: true);

    private void WriteLog(string level, string message,
        Exception? ex = null, object? data = null,
        string? userId = null, bool isAudit = false)
    {
        var prefix = isAudit ? "audit" : "app";
        var file = Path.Combine(_logDirectory,
            $"{prefix}-{DateTime.UtcNow:yyyy-MM-dd}.txt");

        var entry = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            level,
            message,
            userId,
            data,
            exception = ex?.ToString()
        };

        File.AppendAllText(file,
            JsonSerializer.Serialize(entry) + Environment.NewLine);
    }

    private void PurgeLogs(object? _)
    {
        foreach (var file in Directory.GetFiles(_logDirectory, "app-*.txt"))
            if (File.GetCreationTimeUtc(file) < DateTime.UtcNow.AddDays(-AppLogRetentionDays))
                File.Delete(file);

        foreach (var file in Directory.GetFiles(_logDirectory, "audit-*.txt"))
            if (File.GetCreationTimeUtc(file) < DateTime.UtcNow.AddDays(-AuditLogRetentionDays))
                File.Delete(file);
    }

    public void Dispose() => _purgeTimer.Dispose();
}
```

## Logging TypeScript/Node.js

```typescript
import * as fs from 'fs';
import * as path from 'path';

type LogLevel = 'DEBUG' | 'INFO' | 'WARNING' | 'ERROR' | 'CRITICAL';

class Logger {
  private logDir: string;

  constructor(logDir = './logs') {
    this.logDir = logDir;
    fs.mkdirSync(logDir, { recursive: true });
  }

  private write(level: LogLevel, message: string, data?: unknown, error?: Error): void {
    const entry = {
      timestamp: new Date().toISOString(),
      level,
      message,
      data,
      error: error?.stack,
    };
    const file = path.join(this.logDir, `app-${new Date().toISOString().slice(0, 10)}.txt`);
    fs.appendFileSync(file, JSON.stringify(entry) + '\n');
  }

  info(message: string, data?: unknown) { this.write('INFO', message, data); }
  warn(message: string, data?: unknown) { this.write('WARNING', message, data); }
  error(message: string, error?: Error, data?: unknown) { this.write('ERROR', message, data, error); }
}

export const logger = new Logger();
```

## Auditoria obrigatória

As seguintes operações devem gerar registro de auditoria:

```
✅ Login / Logout
✅ Criação, alteração e exclusão de registros
✅ Alteração de permissões
✅ Exportação de dados sensíveis
✅ Operações financeiras
✅ Falhas de autenticação consecutivas
```

## GlobalExceptionMiddleware (ASP.NET Core)

```csharp
public class GlobalExceptionMiddleware(RequestDelegate next, ILoggingService logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError("Erro não tratado", ex, new {
                path = context.Request.Path,
                method = context.Request.Method
            });

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new {
                errors = new[] { "Erro interno. Tente novamente." }
            });
        }
    }
}
```

## Regras

```
✅ Logar início e fim de operações críticas (INFO)
✅ Logar todos os erros com contexto suficiente para reproduzir
✅ Rotação diária de arquivos de log
✅ Expurgo automático (30 dias app / 90 dias auditoria)
✅ Auditoria em toda operação de escrita com userId

❌ Console.WriteLine / console.log em produção
❌ Logar senhas, tokens ou dados pessoais
❌ Swallow exceptions sem logar
❌ Logs sem timestamp ou nível
```
