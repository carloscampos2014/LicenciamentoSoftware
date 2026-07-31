using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Encerra sessões ativas que não registraram heartbeat dentro do
/// <c>TempoLimiteSessaoHoras</c> configurado na licença.
/// </summary>
public sealed class EncerrarSessoesInativasJob : IScheduledJob
{
    public string Nome => "EncerrarSessoesInativas";

    private readonly ILicencaSessaoRepository _sessaoRepo;
    private readonly IClock _clock;
    private readonly ILogger<EncerrarSessoesInativasJob> _logger;
    private readonly int _limiteHoras;

    private static readonly Action<ILogger, DateTime, Exception?> _logIniciando =
        LoggerMessage.Define<DateTime>(LogLevel.Information,
            new EventId(1, "EncerrarSessoesInativas_Iniciando"),
            "[EncerrarSessoesInativas] Encerrando sessões sem atividade desde {Limite:u}");

    private static readonly Action<ILogger, int, Exception?> _logConcluido =
        LoggerMessage.Define<int>(LogLevel.Information,
            new EventId(2, "EncerrarSessoesInativas_Concluido"),
            "[EncerrarSessoesInativas] {Total} sessão(ões) encerrada(s).");

    public EncerrarSessoesInativasJob(
        ILicencaSessaoRepository sessaoRepo,
        IClock clock,
        ILogger<EncerrarSessoesInativasJob> logger,
        int limiteHoras = 24)
    {
        _sessaoRepo  = sessaoRepo;
        _clock       = clock;
        _logger      = logger;
        _limiteHoras = limiteHoras;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var limiteAtividade = _clock.UtcNow.AddHours(-_limiteHoras);
        _logIniciando(_logger, limiteAtividade, null);

        var encerradas = await _sessaoRepo.EncerrarSessoesInativasAsync(limiteAtividade, cancellationToken);
        _logConcluido(_logger, encerradas, null);
    }
}
