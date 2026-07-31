using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Desativa licenças Por Período cujo <c>DataFim</c> já passou e que
/// não têm renovação automática habilitada.
/// </summary>
public sealed class ExpirarLicencasPeriodoJob : IScheduledJob
{
    public string Nome => "ExpirarLicencasPeriodo";

    private readonly ILicencaGestaoRepository _licencaRepo;
    private readonly IClock _clock;
    private readonly ILogger<ExpirarLicencasPeriodoJob> _logger;

    private static readonly Action<ILogger, string, Exception?> _logNenhuma =
        LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(1, "ExpirarLicencas_Nenhuma"),
            "[{Job}] Nenhuma licença vencida encontrada.");

    private static readonly Action<ILogger, string, int, string, Exception?> _logDesativadas =
        LoggerMessage.Define<string, int, string>(LogLevel.Information,
            new EventId(2, "ExpirarLicencas_Desativadas"),
            "[{Job}] {Total} licença(s) desativada(s) por vencimento: {Ids}");

    public ExpirarLicencasPeriodoJob(
        ILicencaGestaoRepository licencaRepo,
        IClock clock,
        ILogger<ExpirarLicencasPeriodoJob> logger)
    {
        _licencaRepo = licencaRepo;
        _clock       = clock;
        _logger      = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var agora   = _clock.UtcNow;
        var vencidas = await _licencaRepo.BuscarLicencasPeriodoVencidasAsync(agora, cancellationToken);

        if (vencidas.Count == 0)
        {
            _logNenhuma(_logger, Nome, null);
            return;
        }

        var ids = vencidas.Select(l => l.IdLicenca).ToList();
        await _licencaRepo.DesativarLicencasPeriodoVencidasAsync(ids, cancellationToken);
        _logDesativadas(_logger, Nome, ids.Count, string.Join(", ", ids), null);
    }
}
