using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class LogoutValidacaoHandlerTests
{
    private readonly ILicencaSessaoRepository _sessaoRepo = Substitute.For<ILicencaSessaoRepository>();
    private readonly IUnitOfWork              _uow        = Substitute.For<IUnitOfWork>();

    private LogoutValidacaoHandler CriarHandler() => new(_sessaoRepo, _uow);

    private static readonly Guid IdLicenca = Guid.NewGuid();
    private static readonly Guid IdSessao  = Guid.NewGuid();

    private static SessaoResult SessaoAtiva() => new(
        IdSessao, IdLicenca, "usuario@teste.com",
        DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(-5), Ativo: true);

    // -------------------------------------------------------------------------
    // Sessão não encontrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoNaoEncontrada_RetornaSessaoNaoEncontrada()
    {
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns((SessaoResult?)null);

        var resultado = await CriarHandler().HandleAsync(
            new LogoutValidacaoCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<LogoutValidacaoResult.SessaoNaoEncontrada>();
        await _uow.DidNotReceive().BeginAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Tenant isolation — sessão de outra licença
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoDeOutraLicenca_RetornaAcessoNegado()
    {
        var outraLicenca = Guid.NewGuid();
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns(new SessaoResult(IdSessao, outraLicenca, "u@x.com",
                DateTime.UtcNow, DateTime.UtcNow, Ativo: true));

        var resultado = await CriarHandler().HandleAsync(
            new LogoutValidacaoCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<LogoutValidacaoResult.AcessoNegado>();
    }

    // -------------------------------------------------------------------------
    // Idempotência — sessão já encerrada = sucesso sem novas escritas
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoJaEncerrada_RetornaSucessoSemEscritas()
    {
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns(SessaoAtiva() with { Ativo = false });

        var resultado = await CriarHandler().HandleAsync(
            new LogoutValidacaoCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<LogoutValidacaoResult.Sucesso>();
        // Idempotente: não deve abrir transação nem chamar EncerrarAsync
        await _uow.DidNotReceive().BeginAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>());
        await _sessaoRepo.DidNotReceive().EncerrarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Sucesso — sessão ativa encerrada normalmente
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoAtiva_EncerraERetornaSucesso()
    {
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns(SessaoAtiva());

        var resultado = await CriarHandler().HandleAsync(
            new LogoutValidacaoCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<LogoutValidacaoResult.Sucesso>();
        await _uow.Received(1).BeginAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>());
        await _sessaoRepo.Received(1).EncerrarAsync(IdSessao, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
