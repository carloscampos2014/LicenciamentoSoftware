using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Handlers;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Aplicacao;

public class CriarAplicacaoHandlerTests
{
    private readonly IAplicacaoRepository _repo = Substitute.For<IAplicacaoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CriarAplicacaoHandler CriarHandler() => new(_repo, _uow);

    private static readonly Guid IdTipoLicenca = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static CriarAplicacaoCommand CommandValido() => new(
        Guid.NewGuid(), "Meu Software", IdTipoLicenca, "Descrição opcional");

    [Fact]
    public async Task Handle_DadosInvalidos_RetornaInvalido()
    {
        var command = CommandValido() with { Titulo = "" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarAplicacaoResult.Invalido>();
    }

    [Fact]
    public async Task Handle_TipoLicencaInexistente_RetornaTipoNaoEncontrado()
    {
        _repo.ExisteTipoLicencaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarAplicacaoResult.TipoLicencaNaoEncontrado>();
    }

    [Fact]
    public async Task Handle_Sucesso_RetornaAplicacaoCriada()
    {
        _repo.ExisteTipoLicencaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.InserirAsync(Arg.Any<Domain.Entities.Aplicacao>(), Arg.Any<CancellationToken>())
             .Returns(x => ((Domain.Entities.Aplicacao)x[0]).Id);
        // BuscarPorIdAsync é chamado após o insert para obter TipoLicencaDescricao
        _repo.BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(x => new AplicacaoResult(
                 (Guid)x[0], Guid.NewGuid(), "Meu Software", "Descrição opcional",
                 IdTipoLicenca, "Permanente", true));

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarAplicacaoResult.Sucesso>();
        var sucesso = (CriarAplicacaoResult.Sucesso)resultado;
        sucesso.Aplicacao.Titulo.Should().Be("Meu Software");
        sucesso.Aplicacao.IdTipoLicenca.Should().Be(IdTipoLicenca);
        sucesso.Aplicacao.TipoLicencaDescricao.Should().Be("Permanente");
    }

    [Fact]
    public async Task Handle_Sucesso_ChamaUoW()
    {
        _repo.ExisteTipoLicencaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.InserirAsync(Arg.Any<Domain.Entities.Aplicacao>(), Arg.Any<CancellationToken>())
             .Returns(Guid.NewGuid());

        await CriarHandler().HandleAsync(CommandValido());

        await _uow.Received(1).BeginAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TituloMaiorQue120_RetornaInvalido()
    {
        var command = CommandValido() with { Titulo = new string('A', 121) };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarAplicacaoResult.Invalido>();
    }
}
