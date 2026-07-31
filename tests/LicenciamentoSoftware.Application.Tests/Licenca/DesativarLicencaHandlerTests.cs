using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class DesativarLicencaHandlerTests
{
    private readonly ILicencaGestaoRepository _repo = Substitute.For<ILicencaGestaoRepository>();
    private readonly IUnitOfWork              _uow  = Substitute.For<IUnitOfWork>();

    private DesativarLicencaHandler CriarHandler() => new(_repo, _uow);

    private static readonly Guid IdTenant = Guid.NewGuid();

    private static LicencaResult LicencaAtiva() => new(
        Guid.NewGuid(), IdTenant, Guid.NewGuid(), "Cliente Teste",
        Guid.NewGuid(), "App Teste",
        Guid.Parse("11111111-1111-1111-1111-111111111111"), "Permanente",
        DateTime.UtcNow, true, null, null, null, null, null);

    [Fact]
    public async Task Handle_LicencaNaoEncontrada_RetornaNaoEncontrado()
    {
        _repo.BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((LicencaResult?)null);

        var resultado = await CriarHandler().HandleAsync(Guid.NewGuid());

        resultado.Should().BeOfType<DesativarLicencaResult.NaoEncontrado>();
    }

    [Fact]
    public async Task Handle_LicencaJaInativa_RetornaJaInativo()
    {
        var inativa = LicencaAtiva() with { Ativo = false };
        _repo.BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(inativa);

        var resultado = await CriarHandler().HandleAsync(Guid.NewGuid());

        resultado.Should().BeOfType<DesativarLicencaResult.JaInativo>();
    }

    [Fact]
    public async Task Handle_LicencaAtiva_RetornaSucesso()
    {
        _repo.BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(LicencaAtiva());

        var resultado = await CriarHandler().HandleAsync(Guid.NewGuid());

        resultado.Should().BeOfType<DesativarLicencaResult.Sucesso>();
        await _repo.Received(1).DesativarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
