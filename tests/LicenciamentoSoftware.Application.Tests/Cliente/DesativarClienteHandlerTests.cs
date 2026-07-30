using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Handlers;
using LicenciamentoSoftware.Application.Cliente.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Cliente;

public class DesativarClienteHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DesativarClienteHandler CriarHandler() => new(_repo, _uow);

    private static Application.Cliente.Results.ClienteResult ClienteAtivo() => new(
        Guid.NewGuid(), "Empresa", 2, "12345678000195", "email@empresa.com", null, true);

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_RetornaNaoEncontrado()
    {
        _repo.BuscarPorIdAsync(Arg.Any<Guid>()).Returns((ClienteResult?)null);

        var resultado = await CriarHandler().HandleAsync(Guid.NewGuid());

        resultado.Should().BeOfType<DesativarClienteResult.NaoEncontrado>();
    }

    [Fact]
    public async Task Handle_ClienteJaInativo_RetornaJaInativo()
    {
        var clienteInativo = ClienteAtivo() with { Ativo = false };
        _repo.BuscarPorIdAsync(Arg.Any<Guid>()).Returns(clienteInativo);

        var resultado = await CriarHandler().HandleAsync(Guid.NewGuid());

        resultado.Should().BeOfType<DesativarClienteResult.JaInativo>();
    }

    [Fact]
    public async Task Handle_ClienteAtivo_RetornaSucesso()
    {
        _repo.BuscarPorIdAsync(Arg.Any<Guid>()).Returns(ClienteAtivo());

        var resultado = await CriarHandler().HandleAsync(Guid.NewGuid());

        resultado.Should().BeOfType<DesativarClienteResult.Sucesso>();
        await _repo.Received(1).DesativarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
