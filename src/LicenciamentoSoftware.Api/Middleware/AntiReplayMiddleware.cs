using LicenciamentoSoftware.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace LicenciamentoSoftware.Api.Middleware;

/// <summary>
/// Middleware anti-replay para endpoints de validação de licença.
/// Rejeita requisições cujo timestamp esteja fora da janela configurada (padrão ±5 min)
/// ou cujo nonce já tenha sido processado dentro dessa janela.
/// </summary>
/// <remarks>
/// Headers esperados:
/// <list type="bullet">
///   <item><c>X-Timestamp</c> — data/hora UTC no formato ISO-8601 (ex: 2026-07-30T12:00:00Z)</item>
///   <item><c>X-Nonce</c> — identificador único da requisição (UUID ou string aleatória)</item>
/// </list>
/// </remarks>
public sealed class AntiReplayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AntiReplayMiddleware> _logger;
    private readonly AntiReplayOptions _options;

    // CA1848 — delegates de log em tempo de compilação para melhor desempenho
    private static readonly Action<ILogger, string, Exception?> _logTimestampAusente =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, "TimestampAusente"),
            "AntiReplay: header X-Timestamp ausente. Path={Path}");

    private static readonly Action<ILogger, string, Exception?> _logTimestampInvalido =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, "TimestampInvalido"),
            "AntiReplay: X-Timestamp inválido: {Timestamp}");

    private static readonly Action<ILogger, DateTimeOffset, DateTimeOffset, int, Exception?> _logTimestampForaJanela =
        LoggerMessage.Define<DateTimeOffset, DateTimeOffset, int>(LogLevel.Warning, new EventId(3, "TimestampForaJanela"),
            "AntiReplay: timestamp fora da janela. Recebido={Timestamp}, Agora={Agora}, Janela={Janela}min");

    private static readonly Action<ILogger, string, Exception?> _logNonceAusente =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, "NonceAusente"),
            "AntiReplay: header X-Nonce ausente. Path={Path}");

    private static readonly Action<ILogger, string, Exception?> _logNonceDuplicado =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5, "NonceDuplicado"),
            "AntiReplay: nonce duplicado detectado. Nonce={Nonce}");

    public AntiReplayMiddleware(
        RequestDelegate next,
        ILogger<AntiReplayMiddleware> logger,
        IOptions<AntiReplayOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, INonceRepository nonceRepository)
    {
        // 1. Lê e valida o header X-Timestamp
        if (!context.Request.Headers.TryGetValue("X-Timestamp", out var timestampRaw)
            || string.IsNullOrWhiteSpace(timestampRaw))
        {
            _logTimestampAusente(_logger, context.Request.Path, null);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Erro = "Header X-Timestamp é obrigatório." });
            return;
        }

        if (!DateTimeOffset.TryParse(timestampRaw, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp))
        {
            _logTimestampInvalido(_logger, timestampRaw.ToString(), null);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Erro = "Header X-Timestamp deve estar no formato ISO-8601 UTC." });
            return;
        }

        var agora = DateTimeOffset.UtcNow;
        var janela = TimeSpan.FromMinutes(_options.JanelaMinutos);

        if (timestamp < agora - janela || timestamp > agora + janela)
        {
            _logTimestampForaJanela(_logger, timestamp, agora, _options.JanelaMinutos, null);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Erro = $"Timestamp fora da janela permitida (±{_options.JanelaMinutos} minutos)." });
            return;
        }

        // 2. Lê e valida o header X-Nonce
        if (!context.Request.Headers.TryGetValue("X-Nonce", out var nonceRaw)
            || string.IsNullOrWhiteSpace(nonceRaw))
        {
            _logNonceAusente(_logger, context.Request.Path, null);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Erro = "Header X-Nonce é obrigatório." });
            return;
        }

        var nonce = nonceRaw.ToString().Trim();

        if (nonce.Length > 128)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Erro = "X-Nonce não pode exceder 128 caracteres." });
            return;
        }

        // 3. Verifica replay
        var replay = await nonceRepository.ExisteAsync(nonce, context.RequestAborted);

        if (replay)
        {
            _logNonceDuplicado(_logger, nonce, null);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Erro = "Requisição duplicada (nonce já utilizado)." });
            return;
        }

        // 4. Registra o nonce antes de processar — expira junto com a janela
        var expiraEm = agora.Add(janela).UtcDateTime;
        await nonceRepository.RegistrarAsync(nonce, expiraEm, context.RequestAborted);

        await _next(context);
    }
}

/// <summary>Configuração do middleware anti-replay, lida de <c>LicencaTokenSettings</c>.</summary>
public sealed class AntiReplayOptions
{
    /// <summary>Janela de tempo em minutos para aceitar o timestamp (padrão: 5).</summary>
    public int JanelaMinutos { get; set; } = 5;
}
