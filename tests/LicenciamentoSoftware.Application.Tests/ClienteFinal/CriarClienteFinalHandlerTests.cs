using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Commands;
using LicenciamentoSoftware.Application.ClienteFinal.Handlers;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.ClienteFinal;

public class CriarClienteFinalHandlerTests
{
    private readonly IClienteFinalRepository _repo = Substitute.For<IClienteFinalRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CriarClienteFinalHandler CriarHandler() => new(_repo, _uow);

    private static CriarClienteFinalCommand CommandValido() => new(
        Guid.NewGuid(), "Cliente Final Ltda", 2, "11222333000181", "cf@empresa.com", null);

    [Fact]
    public async Task Handle_DadosInvalidos_RetornaInvalido()
    {
        var command = CommandValido() with { RazaoSocial = "" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarClienteFinalResult.Invalido>();
    }

    [Fact]
    public async Task Handle_InscricaoDuplicada_RetornaInscricaoJaExiste()
    {
        _repo.ExisteInscricaoAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarClienteFinalResult.InscricaoJaExiste>();
    }

    [Fact]
    public async Task Handle_Sucesso_RetornaClienteFinalCriado()
    {
        _repo.ExisteInscricaoAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.InserirAsync(Arg.Any<Domain.Entities.ClienteFinal>(), Arg.Any<CancellationToken>())
             .Returns(x => ((Domain.Entities.ClienteFinal)x[0]).Id);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarClienteFinalResult.Sucesso>();
        var sucesso = (CriarClienteFinalResult.Sucesso)resultado;
        sucesso.ClienteFinal.RazaoSocial.Should().Be("Cliente Final Ltda");
    }

    [Fact]
    public async Task Handle_IdClienteVazio_RetornaInvalido()
    {
        var command = CommandValido() with { IdCliente = Guid.Empty };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarClienteFinalResult.Invalido>();
    }
}
