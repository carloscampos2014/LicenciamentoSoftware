using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Api.Middleware;

/// <summary>
/// Handler centralizado de exceções não tratadas.
/// Transforma qualquer exceção em ProblemDetails sem expor stack trace em produção.
/// Registrado via app.UseExceptionHandler() no pipeline.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // CA1848: usar LoggerMessage source-generated para alta performance.
    private static readonly Action<ILogger, string, string, Exception?> LogUnhandledException =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(GlobalExceptionHandler)),
            "Exceção não tratada: {Message}. TraceId: {TraceId}");

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(_logger, exception.Message, httpContext.TraceIdentifier, exception);

        var problemDetails = new ProblemDetails
        {
            Status   = StatusCodes.Status500InternalServerError,
            Title    = "Erro interno do servidor",
            Detail   = "Ocorreu um erro inesperado. Tente novamente mais tarde.",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier,
            },
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Retorna true: exceção foi tratada, o pipeline não propaga.
        return true;
    }
}
