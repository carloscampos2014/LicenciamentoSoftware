using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Handlers;
using LicenciamentoSoftware.Application.Dashboard.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Dashboard;

public class BuscarDashboardAlertasHandlerTests
{
    private static readonly Guid IdCliente = Guid.NewGuid();

    private readonly IDashboardRepository _repo = Substitute.For<IDashboardRepository>();
    private readonly ICurrentUser _currentUser   = Substitute.For<ICurrentUser>();

    private BuscarDashboardAlertasHandler CriarHandler()
    {
        _currentUser.IdCliente.Returns(IdCliente);
        return new BuscarDashboardAlertasHandler(_repo, _currentUser);
    }

    private static DashboardAlertasResult AlertasFake() => new(
        SessoesInativas:        [],
        InstalacoesAdormecidas: [],
        LicencasNoLimite:       [],
        ErrosValidacao: new ErrosValidacaoAlerta(0, [], []));

    [Fact]
    public async Task Handle_RetornaAlertasDoTenant()
    {
        var esperado = AlertasFake();
        _repo.BuscarAlertasAsync(IdCliente, Arg.Any<CancellationToken>())
             .Returns(esperado);

        var resultado = await CriarHandler().HandleAsync();

        resultado.Should().BeEquivalentTo(esperado);
    }

    [Fact]
    public async Task Handle_UsaIdClienteDoCurrentUser()
    {
        _repo.BuscarAlertasAsync(IdCliente, Arg.Any<CancellationToken>())
             .Returns(AlertasFake());

        await CriarHandler().HandleAsync();

        await _repo.Received(1).BuscarAlertasAsync(IdCliente, Arg.Any<CancellationToken>());
    }
}
