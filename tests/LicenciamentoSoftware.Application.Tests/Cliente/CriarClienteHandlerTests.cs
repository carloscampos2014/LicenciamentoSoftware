using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Handlers;
using LicenciamentoSoftware.Application.Cliente.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Cliente;

public class CriarClienteHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CriarClienteHandler CriarHandler() => new(_repo, _uow);

    // CNPJ válido para testes
    private static CriarClienteCommand CommandValido() => new(
        "Empresa Teste Ltda", 2, "11222333000181", "contato@empresa.com", null);

    [Fact]
    public async Task Handle_DadosInvalidos_RetornaInvalido()
    {
        var command = CommandValido() with { RazaoSocial = "" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarClienteResult.Invalido>();
        ((CriarClienteResult.Invalido)resultado).Erros.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_InscricaoDuplicada_RetornaInscricaoJaExiste()
    {
        _repo.ExisteInscricaoAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarClienteResult.InscricaoJaExiste>();
    }

    [Fact]
    public async Task Handle_Sucesso_RetornaSucessoComId()
    {
        _repo.ExisteInscricaoAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);
        _repo.InserirAsync(Arg.Any<Domain.Entities.Cliente>(), Arg.Any<CancellationToken>())
             .Returns(x => ((Domain.Entities.Cliente)x[0]).Id);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarClienteResult.Sucesso>();
        var sucesso = (CriarClienteResult.Sucesso)resultado;
        sucesso.Cliente.Id.Should().NotBe(Guid.Empty);
        sucesso.Cliente.RazaoSocial.Should().Be("Empresa Teste Ltda");
    }

    [Fact]
    public async Task Handle_Sucesso_ChamaUoW()
    {
        _repo.ExisteInscricaoAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);
        _repo.InserirAsync(Arg.Any<Domain.Entities.Cliente>(), Arg.Any<CancellationToken>())
             .Returns(Guid.NewGuid());

        await CriarHandler().HandleAsync(CommandValido());

        await _uow.Received(1).BeginAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CnpjInvalido_RetornaInvalido()
    {
        // CNPJ com formato válido mas dígitos errados
        var command = CommandValido() with { NumeroInscricao = "11111111111111" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarClienteResult.Invalido>();
    }
}
