using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class ValidarLoginHandlerTests
{
    private static readonly Guid TipoPermanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoPeriodo    = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TipoUsuarios   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TipoInstalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Guid IdLicenca = Guid.NewGuid();
    private const string IdentificadorUsuario = "usuario@empresa.com";

    private readonly IValidacaoLicencaRepository _validacaoRepo =
        Substitute.For<IValidacaoLicencaRepository>();
    private readonly ILicencaSessaoRepository _sessaoRepo =
        Substitute.For<ILicencaSessaoRepository>();
    private readonly IUnitOfWork _uow  = Substitute.For<IUnitOfWork>();
    private readonly IClock      _clock = Substitute.For<IClock>();

    private ValidarLoginHandler CriarHandler() =>
        new(_validacaoRepo, _sessaoRepo, _uow, _clock);

    private static ValidarLoginCommand Command(string usuario = IdentificadorUsuario) =>
        new(IdLicenca, usuario);

    // -------------------------------------------------------------------------
    // Validação de entrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_IdLicencaVazio_RetornaInvalido()
    {
        var resultado = await CriarHandler().HandleAsync(new ValidarLoginCommand(Guid.Empty, "user"));

        resultado.Should().BeOfType<ValidarLoginResult.Invalido>();
    }

    [Fact]
    public async Task Handle_IdentificadorVazio_RetornaInvalido()
    {
        var resultado = await CriarHandler().HandleAsync(new ValidarLoginCommand(IdLicenca, ""));

        resultado.Should().BeOfType<ValidarLoginResult.Invalido>();
    }

    [Fact]
    public async Task Handle_IdentificadorMuitoLongo_RetornaInvalido()
    {
        var resultado = await CriarHandler().HandleAsync(
            new ValidarLoginCommand(IdLicenca, new string('x', 301)));

        resultado.Should().BeOfType<ValidarLoginResult.Invalido>();
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

        resultado.Should().BeOfType<ValidarLoginResult.LicencaNaoEncontrada>();
    }

    [Fact]
    public async Task Handle_LicencaInativa_RetornaLicencaInativa()
    {
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfo(TipoPermanente, ativo: false));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.LicencaInativa>();
    }

    // -------------------------------------------------------------------------
    // Tipo Permanente — autoriza diretamente sem sessão
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaPermanente_AutorizaSemSessao()
    {
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfo(TipoPermanente));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.Sucesso>()
            .Which.IdSessao.Should().BeNull();
        await _sessaoRepo.DidNotReceive().InserirAsync(Arg.Any<Domain.Entities.LicencaSessao>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Tipo Por Período
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaPeriodoAtivo_AutorizaSemSessao()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc));
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfo(TipoPeriodo,
                dataFim: new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.Sucesso>()
            .Which.IdSessao.Should().BeNull();
    }

    [Fact]
    public async Task Handle_LicencaPeriodoExpirado_RetornaLicencaExpirada()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc));
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfo(TipoPeriodo,
                dataFim: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.LicencaExpirada>();
    }

    [Fact]
    public async Task Handle_LicencaPeriodoDataFimNula_RetornaExpirada()
    {
        _clock.UtcNow.Returns(DateTime.UtcNow);
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfo(TipoPeriodo, dataFim: null));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.LicencaExpirada>();
    }

    // -------------------------------------------------------------------------
    // Tipo Por Usuários — dentro do limite
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaUsuarios_DentroDoLimite_RetornaSucessoComSessao()
    {
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoUsuarios(quantidadeMaxima: 5, maxPorUsuario: 3));

        _sessaoRepo.ContarAtivasPorLicencaAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(4);  // 4 de 5 usadas
        _sessaoRepo.ContarAtivasPorUsuarioAsync(IdLicenca, IdentificadorUsuario, Arg.Any<CancellationToken>())
            .Returns(0);  // usuário sem sessões

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.Sucesso>()
            .Which.IdSessao.Should().NotBeNull();
        await _sessaoRepo.Received(1)
            .InserirAsync(Arg.Any<Domain.Entities.LicencaSessao>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Tipo Por Usuários — limite global atingido
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaUsuarios_LimiteGlobalAtingido_RetornaLimiteAtingido()
    {
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoUsuarios(quantidadeMaxima: 3, maxPorUsuario: 5));

        _sessaoRepo.ContarAtivasPorLicencaAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(3);  // exatamente no limite

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.LimiteUsuariosAtingido>()
            .Which.QuantidadeMaxima.Should().Be(3);
        await _sessaoRepo.DidNotReceive()
            .InserirAsync(Arg.Any<Domain.Entities.LicencaSessao>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Tipo Por Usuários — limite por usuário atingido
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaUsuarios_LimitePorUsuarioAtingido_RetornaLimitePorUsuario()
    {
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoUsuarios(quantidadeMaxima: 10, maxPorUsuario: 2));

        _sessaoRepo.ContarAtivasPorLicencaAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(5);   // dentro do limite global
        _sessaoRepo.ContarAtivasPorUsuarioAsync(IdLicenca, IdentificadorUsuario, Arg.Any<CancellationToken>())
            .Returns(2);   // exatamente no limite por usuário

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.LimiteSessionsPorUsuarioAtingido>()
            .Which.MaxSessoesPorUsuario.Should().Be(2);
        await _sessaoRepo.DidNotReceive()
            .InserirAsync(Arg.Any<Domain.Entities.LicencaSessao>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Tipo Por Usuários — concorrência no último slot
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaUsuarios_ConcorrenciaUltimoSlot_SegundoRetornaLimite()
    {
        // Simula: dois handlers consultam simultaneamente e ambos veem 4 de 5.
        // O segundo chama também ContarAtivasPorLicencaAsync e vê 5 (após o primeiro ter inserido).
        // Esse teste verifica o comportamento do handler quando o contador já está no limite.
        ConfigurarUowParaTransacao();
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfoUsuarios(quantidadeMaxima: 5, maxPorUsuario: 10));

        // Segunda chamada vê o slot cheio (simulando que o primeiro handler já inseriu)
        _sessaoRepo.ContarAtivasPorLicencaAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(5);

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.LimiteUsuariosAtingido>();
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Tipo Por Instalação — incompatível com este endpoint
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaInstalacao_RetornaTipoIncompativel()
    {
        _validacaoRepo.BuscarParaValidacaoAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(LicencaInfo(TipoInstalacao));

        var resultado = await CriarHandler().HandleAsync(Command());

        resultado.Should().BeOfType<ValidarLoginResult.TipoLicencaIncompativel>();
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

    private static LicencaValidacaoInfo LicencaInfo(
        Guid idTipo,
        bool ativo = true,
        DateTime? dataFim = null) =>
        new(IdLicenca, Guid.NewGuid(), ativo, idTipo,
            dataFim, null, null, null, null, null);

    private static LicencaValidacaoInfo LicencaInfoUsuarios(
        int quantidadeMaxima, int maxPorUsuario, int tempoLimiteSessaoHoras = 24) =>
        new(IdLicenca, Guid.NewGuid(), true, TipoUsuarios,
            null, null,
            quantidadeMaxima, maxPorUsuario, tempoLimiteSessaoHoras,
            null);
}
