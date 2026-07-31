using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Jobs;

public class ExpirarLicencasPeriodoJobTests
{
    private readonly ILicencaGestaoRepository _licencaRepo = Substitute.For<ILicencaGestaoRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private ExpirarLicencasPeriodoJob CriarJob() =>
        new(_licencaRepo, _clock, NullLogger<ExpirarLicencasPeriodoJob>.Instance);

    private static readonly DateTime Agora =
        new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Execute_LicencasVencidas_DesativaEmLote()
    {
        _clock.UtcNow.Returns(Agora);

        var vencidas = new List<LicencaPeriodoJobInfo>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "App A",
                Agora.AddYears(-1), Agora.AddDays(-5), RenovacaoAutomatica: false),
            new(Guid.NewGuid(), Guid.NewGuid(), "App B",
                Agora.AddYears(-1), Agora.AddDays(-1), RenovacaoAutomatica: false),
        };

        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Agora, Arg.Any<CancellationToken>())
            .Returns(vencidas);

        await CriarJob().ExecuteAsync();

        await _licencaRepo.Received(1).DesativarLicencasPeriodoVencidasAsync(
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_NenhumaLicencaVencida_NaoDesativa()
    {
        _clock.UtcNow.Returns(Agora);
        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Agora, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());

        await CriarJob().ExecuteAsync();

        await _licencaRepo.DidNotReceive().DesativarLicencasPeriodoVencidasAsync(
            Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_UsaHorarioDoRelogio_NaoDateTimeUtcNow()
    {
        var horarioMock = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(horarioMock);
        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());

        await CriarJob().ExecuteAsync();

        await _licencaRepo.Received(1)
            .BuscarLicencasPeriodoVencidasAsync(horarioMock, Arg.Any<CancellationToken>());
    }
}
