using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Handlers;
using LicenciamentoSoftware.Application.Dashboard.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Dashboard;

public class BuscarDashboardResumoHandlerTests
{
    private static readonly Guid IdCliente = Guid.NewGuid();

    private readonly IDashboardRepository _repo = Substitute.For<IDashboardRepository>();
    private readonly ICurrentUser _currentUser   = Substitute.For<ICurrentUser>();

    private BuscarDashboardResumoHandler CriarHandler()
    {
        _currentUser.IdCliente.Returns(IdCliente);
        return new BuscarDashboardResumoHandler(_repo, _currentUser);
    }

    private static DashboardResumoResult ResumoFake() => new(
        TotalClientesFinaisAtivos:        5,
        TotalAplicacoesAtivas:            3,
        TotalLicencasAtivas:              10,
        TotalLicencasInativas:            2,
        LicencasPorTipo: new LicencasPorTipoResult(3, 4, 2, 1),
        LicencasExpirandoEm7Dias:         1,
        SessoesAtivasAgora:               6,
        TokensExpirandoEm7Dias:           0,
        NovasLicencasUltimos30Dias:        3,
        NovosClientesFinaisUltimos30Dias:  2);

    [Fact]
    public async Task Handle_RetornaResumoDoTenant()
    {
        var esperado = ResumoFake();
        _repo.BuscarResumoAsync(IdCliente, Arg.Any<CancellationToken>())
             .Returns(esperado);

        var resultado = await CriarHandler().HandleAsync();

        resultado.Should().BeEquivalentTo(esperado);
    }

    [Fact]
    public async Task Handle_UsaIdClienteDoCurrentUser()
    {
        _repo.BuscarResumoAsync(IdCliente, Arg.Any<CancellationToken>())
             .Returns(ResumoFake());

        await CriarHandler().HandleAsync();

        await _repo.Received(1).BuscarResumoAsync(IdCliente, Arg.Any<CancellationToken>());
    }
}
