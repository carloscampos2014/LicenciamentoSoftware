using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Jobs;

public class EncerrarSessoesInativasJobTests
{
    private readonly ILicencaSessaoRepository _sessaoRepo = Substitute.For<ILicencaSessaoRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private EncerrarSessoesInativasJob CriarJob(int limiteHoras = 24) =>
        new(_sessaoRepo, _clock, NullLogger<EncerrarSessoesInativasJob>.Instance, limiteHoras);

    private static readonly DateTime Agora =
        new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Execute_SessoesSemHeartbeat_ChamaEncerrarComLimiteCorreto()
    {
        _clock.UtcNow.Returns(Agora);
        _sessaoRepo.EncerrarSessoesInativasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(3);

        await CriarJob(limiteHoras: 24).ExecuteAsync();

        var limiteEsperado = Agora.AddHours(-24);
        await _sessaoRepo.Received(1)
            .EncerrarSessoesInativasAsync(limiteEsperado, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_LimitePersonalizado_UsaHorasConfiguradas()
    {
        _clock.UtcNow.Returns(Agora);
        _sessaoRepo.EncerrarSessoesInativasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);

        await CriarJob(limiteHoras: 8).ExecuteAsync();

        var limiteEsperado = Agora.AddHours(-8);
        await _sessaoRepo.Received(1)
            .EncerrarSessoesInativasAsync(limiteEsperado, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_NenhumaSessaoInativa_NaoFalha()
    {
        _clock.UtcNow.Returns(Agora);
        _sessaoRepo.EncerrarSessoesInativasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var act = () => CriarJob().ExecuteAsync();

        await act.Should().NotThrowAsync();
    }
}
