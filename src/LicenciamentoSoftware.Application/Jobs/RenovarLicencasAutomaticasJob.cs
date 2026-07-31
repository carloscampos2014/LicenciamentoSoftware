using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>
/// Estende o <c>DataFim</c> de licenças Por Período com
/// <c>RenovacaoAutomatica = true</c> que estão próximas do vencimento.
/// A extensão é pelo mesmo número de dias da duração original da licença.
/// </summary>
public sealed class RenovarLicencasAutomaticasJob : IScheduledJob
{
    public string Nome => "RenovarLicencasAutomaticas";

    private readonly ILicencaGestaoRepository _licencaRepo;
    private readonly IClock _clock;
    private readonly ILogger<RenovarLicencasAutomaticasJob> _logger;
    private readonly int _diasAntecedencia;

    private static readonly Action<ILogger, string, Exception?> _logNenhuma =
        LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(1, "RenovarAuto_Nenhuma"),
            "[{Job}] Nenhuma licença candidata à renovação automática.");

    private static readonly Action<ILogger, string, Guid, string, string, string, Exception?> _logRenovada =
        LoggerMessage.Define<string, Guid, string, string, string>(LogLevel.Information,
            new EventId(2, "RenovarAuto_Renovada"),
            "[{Job}] Licença {IdLicenca} ({App}) renovada automaticamente. DataFim anterior: {DataFimAnterior} → Nova: {NovaDataFim}");

    public RenovarLicencasAutomaticasJob(
        ILicencaGestaoRepository licencaRepo,
        IClock clock,
        ILogger<RenovarLicencasAutomaticasJob> logger,
        int diasAntecedencia = 7)
    {
        _licencaRepo      = licencaRepo;
        _clock            = clock;
        _logger           = logger;
        _diasAntecedencia = diasAntecedencia;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var agora      = _clock.UtcNow;
        var candidatas = await _licencaRepo.BuscarLicencasRenovacaoAutomaticaAsync(
            agora, _diasAntecedencia, cancellationToken);

        if (candidatas.Count == 0)
        {
            _logNenhuma(_logger, Nome, null);
            return;
        }

        foreach (var licenca in candidatas)
        {
            var duracaoDias = (licenca.DataFim - licenca.DataInicio).Days;
            if (duracaoDias <= 0) duracaoDias = 365;

            var novaDataFim = licenca.DataFim.AddDays(duracaoDias);
            await _licencaRepo.RenovarDataFimLicencaAsync(licenca.IdLicenca, novaDataFim, cancellationToken);

            _logRenovada(_logger, Nome, licenca.IdLicenca, licenca.NomeAplicacao,
                licenca.DataFim.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                novaDataFim.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                null);
        }
    }
}
