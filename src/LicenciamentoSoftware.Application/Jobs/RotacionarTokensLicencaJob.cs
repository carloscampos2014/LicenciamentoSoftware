using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Renova automaticamente tokens HMAC de licença que estão próximos do vencimento.
/// Reutiliza o <see cref="RenovarTokenLicencaHandler"/> da Fase 4.
/// </summary>
public sealed class RotacionarTokensLicencaJob : IScheduledJob
{
    public string Nome => "RotacionarTokensLicenca";

    private readonly ILicencaTokenRepository _tokenRepo;
    private readonly RenovarTokenLicencaHandler _renovarHandler;
    private readonly ILogger<RotacionarTokensLicencaJob> _logger;
    private readonly int _diasAntecedencia;

    private static readonly Action<ILogger, string, Exception?> _logNenhum =
        LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(1, "RotacaoToken_Nenhum"),
            "[{Job}] Nenhum token candidato à rotação automática.");

    private static readonly Action<ILogger, string, Guid, string, Exception?> _logRotacionado =
        LoggerMessage.Define<string, Guid, string>(LogLevel.Information,
            new EventId(2, "RotacaoToken_Rotacionado"),
            "[{Job}] Token da licença {IdLicenca} ({App}) rotacionado automaticamente.");

    private static readonly Action<ILogger, string, Guid, string, Exception?> _logFalha =
        LoggerMessage.Define<string, Guid, string>(LogLevel.Warning,
            new EventId(3, "RotacaoToken_Falha"),
            "[{Job}] Falha ao rotacionar token da licença {IdLicenca}: {Resultado}");

    public RotacionarTokensLicencaJob(
        ILicencaTokenRepository tokenRepo,
        RenovarTokenLicencaHandler renovarHandler,
        ILogger<RotacionarTokensLicencaJob> logger,
        int diasAntecedencia = 7)
    {
        _tokenRepo        = tokenRepo;
        _renovarHandler   = renovarHandler;
        _logger           = logger;
        _diasAntecedencia = diasAntecedencia;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var candidatos = await _tokenRepo.BuscarTokensProximosVencimentoAsync(
            _diasAntecedencia, cancellationToken);

        if (candidatos.Count == 0)
        {
            _logNenhum(_logger, Nome, null);
            return;
        }

        foreach (var token in candidatos)
        {
            var resultado = await _renovarHandler.HandleAsync(
                new RenovarTokenLicencaCommand(token.IdLicenca, token.ExpiracaoMinutos),
                cancellationToken);

            if (resultado is EmitirTokenResult.Sucesso)
                _logRotacionado(_logger, Nome, token.IdLicenca, token.NomeAplicacao, null);
            else
                _logFalha(_logger, Nome, token.IdLicenca, resultado.GetType().Name, null);
        }
    }
}
