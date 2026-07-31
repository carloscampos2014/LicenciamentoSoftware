using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class ValidarInstalacaoHandlerTests
{
    private static readonly Guid TipoPermanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoPeriodo    = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TipoInstalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Guid IdLicenca = Guid.NewGuid();
    private const string IdentificadorMaquina = "MACHINE-001-ABCDEF";

    private readonly IValidacaoLicencaRepository  _validacaoRepo  =
        Substitute.For<IValidacaoLicencaRepository>();
    private readonly ILicencaInstalacaoRepository _instalacaoRepo =
        Substitute.For<ILicencaInstalacaoRepository>();
    private readonly IUnitOfWork _uow   = Substitute.For<IUnitOfWork>();
    private readonly IClock      _clock = Substitute.For<IClock>();

    private ValidarInstalacaoHandler CriarHandler() =>
        new(_validacaoRepo, _instalacaoRepo, _uow, _clock);

    private static ValidarInstalacaoCommand Command(string maquina = IdentificadorMaquina) =>
        new(IdLicenca, maquina);

    // -------------------------------------------------------------------------
    // Validação de entrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_IdLicencaVazio_RetornaInvalido()
    {
        var resultado = await CriarHandler().HandleAsync(
            new ValidarInstalacaoCommand(Guid.Empty, IdentificadorMaquina));

        resultado.Should().BeOfType<ValidarInstalacaoResult.Invalido>();
    }

    [Fact]
    public async Task Handle_IdentificadorMaquinaVazio_RetornaInvalido()
    {
        var resultado = await CriarHandler().HandleAsync(
            new ValidarInstalacaoCommand(IdLicenca, ""));

        resultado.Should().BeOfType<ValidarInstalacaoResult.Invalido>();
    }

    [Fact]
    public async Task Handle_IdentificadorMaquinaMuitoLongo_RetornaInvalido()
    {
        var resultado = await CriarHandler().HandleAsync(
            new ValidarInstalacaoCommand(IdLicenca, new string('x', 301)));

        resultado.Should().BeOfType<ValidarInstalacaoResult.Invalido>();
    }

    // -------------------------------------------------------------------------
    // Licença não encontrada / inativa
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaNaoEncontrada_RetornaLicencaNaoEncontrada()
    {
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns((LicencaValidacaoInfo?)null);

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.LicencaNaoEncontrada>();
    }

    [Fact]
    public async Task Handle_LicencaInativa_RetornaLicencaInativa()
    {
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoInstalacao(quantidadeMaxima: 3, ativo: false));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.LicencaInativa>();
    }

    // -------------------------------------------------------------------------
    // Tipo incompatível
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaPermanente_RetornaTipoIncompativel()
    {
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(new LicencaValidacaoInfo(IdLicenca, Guid.NewGuid(), true,
                TipoPermanente, null, null, null, null, null, null));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.TipoLicencaIncompativel>();
    }

    // -------------------------------------------------------------------------
    // Licença expirada (com período associado)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaExpirada_RetornaLicencaExpirada()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc));
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(new LicencaValidacaoInfo(IdLicenca, Guid.NewGuid(), true,
                TipoInstalacao,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),  // DataFim expirado
                null, null, null, null, 5));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.LicencaExpirada>();
    }

    // -------------------------------------------------------------------------
    // Idempotência — máquina já registrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_MaquinaJaRegistrada_RetornaSucessoComJaRegistradaTrue()
    {
        ConfigurarUowParaTransacao();
        var idInstalacaoExistente = Guid.NewGuid();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoInstalacao(quantidadeMaxima: 3));
        _instalacaoRepo.BuscarRegistradaAtivaAsync(IdLicenca, IdentificadorMaquina, Arg.Any<CancellationToken>())
            .Returns(new InstalacaoRegistradaResult(
                idInstalacaoExistente, IdLicenca, IdentificadorMaquina, DateTime.UtcNow, true));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.Sucesso>()
            .Which.JaRegistrada.Should().BeTrue();
        var sucesso = (ValidarInstalacaoResult.Sucesso)resultado;
        sucesso.IdInstalacao.Should().Be(idInstalacaoExistente);
        await _instalacaoRepo.DidNotReceive()
            .InserirRegistradaAsync(Arg.Any<Domain.Entities.LicencaInstalacaoRegistrada>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Limite de instalações atingido
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LimiteInstalacoesAtingido_RetornaLimiteAtingido()
    {
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoInstalacao(quantidadeMaxima: 2));
        _instalacaoRepo.BuscarRegistradaAtivaAsync(IdLicenca, IdentificadorMaquina, Arg.Any<CancellationToken>())
            .Returns((InstalacaoRegistradaResult?)null);
        _instalacaoRepo.ContarAtivasAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(2);  // exatamente no limite

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.LimiteInstalacoesAtingido>()
            .Which.QuantidadeMaxima.Should().Be(2);
        await _instalacaoRepo.DidNotReceive()
            .InserirRegistradaAsync(Arg.Any<Domain.Entities.LicencaInstalacaoRegistrada>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Nova instalação registrada com sucesso
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_InstalacaoNova_RegistraERetornaSucesso()
    {
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoInstalacao(quantidadeMaxima: 5));
        _instalacaoRepo.BuscarRegistradaAtivaAsync(IdLicenca, IdentificadorMaquina, Arg.Any<CancellationToken>())
            .Returns((InstalacaoRegistradaResult?)null);
        _instalacaoRepo.ContarAtivasAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(3);  // 3 de 5 usadas

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.Sucesso>();
        var sucesso = (ValidarInstalacaoResult.Sucesso)resultado;
        sucesso.JaRegistrada.Should().BeFalse();
        sucesso.IdInstalacao.Should().NotBeEmpty();
        await _instalacaoRepo.Received(1)
            .InserirRegistradaAsync(Arg.Any<Domain.Entities.LicencaInstalacaoRegistrada>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Concorrência no último slot
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ConcorrenciaUltimoSlot_SegundoRetornaLimite()
    {
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoInstalacao(quantidadeMaxima: 2));
        _instalacaoRepo.BuscarRegistradaAtivaAsync(IdLicenca, IdentificadorMaquina, Arg.Any<CancellationToken>())
            .Returns((InstalacaoRegistradaResult?)null);
        // Simula: segundo handler vê o contador já no limite após o primeiro inserir
        _instalacaoRepo.ContarAtivasAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(2);

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarInstalacaoResult.LimiteInstalacoesAtingido>();
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void ConfigurarUowParaTransacao()
    {
        _uow.BeginAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.RollbackAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.Connection.Returns(Substitute.For<System.Data.IDbConnection>());
        _uow.Transaction.Returns(Substitute.For<System.Data.IDbTransaction>());
    }

    private static LicencaValidacaoInfo LicencaInfoInstalacao(
        int quantidadeMaxima, bool ativo = true) =>
        new(IdLicenca, Guid.NewGuid(), ativo, TipoInstalacao,
            null, null, null, null, null,
            quantidadeMaxima);
}
