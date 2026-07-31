using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Jobs;

public class RenovarLicencasAutomaticasJobTests
{
    private readonly ILicencaGestaoRepository _licencaRepo = Substitute.For<ILicencaGestaoRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private RenovarLicencasAutomaticasJob CriarJob(int diasAntecedencia = 7) =>
        new(_licencaRepo, _clock, NullLogger<RenovarLicencasAutomaticasJob>.Instance, diasAntecedencia);

    private static readonly DateTime Agora =
        new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Execute_LicencaComRenovacaoAutomatica_EstendeDataFim()
    {
        _clock.UtcNow.Returns(Agora);

        var dataInicio = Agora.AddYears(-1);
        var dataFim    = Agora.AddDays(5); // vence em 5 dias — dentro da janela de 7
        var idLicenca  = Guid.NewGuid();

        _licencaRepo.BuscarLicencasRenovacaoAutomaticaAsync(Agora, 7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>
            {
                new(idLicenca, Guid.NewGuid(), "App X", dataInicio, dataFim, RenovacaoAutomatica: true),
            });

        await CriarJob().ExecuteAsync();

        // Duração original = (dataFim - dataInicio).Days → nova DataFim = dataFim + duração
        var duracaoDias = (dataFim - dataInicio).Days;
        var novaDataFimEsperada = dataFim.AddDays(duracaoDias);
        await _licencaRepo.Received(1)
            .RenovarDataFimLicencaAsync(idLicenca, novaDataFimEsperada, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_NenhumaCandidataRenovacao_NaoChama()
    {
        _clock.UtcNow.Returns(Agora);
        _licencaRepo.BuscarLicencasRenovacaoAutomaticaAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());

        await CriarJob().ExecuteAsync();

        await _licencaRepo.DidNotReceive()
            .RenovarDataFimLicencaAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_MultiplasLicencas_RenovaTodasIndividualmente()
    {
        _clock.UtcNow.Returns(Agora);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var dataInicio = Agora.AddMonths(-6);
        var dataFim    = Agora.AddDays(3);

        _licencaRepo.BuscarLicencasRenovacaoAutomaticaAsync(Agora, 7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>
            {
                new(id1, Guid.NewGuid(), "App 1", dataInicio, dataFim, true),
                new(id2, Guid.NewGuid(), "App 2", dataInicio, dataFim, true),
            });

        await CriarJob().ExecuteAsync();

        await _licencaRepo.Received(1)
            .RenovarDataFimLicencaAsync(id1, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _licencaRepo.Received(1)
            .RenovarDataFimLicencaAsync(id2, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
