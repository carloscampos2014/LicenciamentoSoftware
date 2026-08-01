using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using FluentAssertions;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class EmitirLicencaHandlerTests
{
    private static readonly Guid TipoPermanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoPeriodo    = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TipoUsuarios   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TipoInstalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Guid IdTenant       = Guid.NewGuid();
    private static readonly Guid IdClienteFinal = Guid.NewGuid();
    private static readonly Guid IdAplicativo   = Guid.NewGuid();

    private readonly ILicencaGestaoRepository _licencaRepo = Substitute.For<ILicencaGestaoRepository>();
    private readonly IClienteFinalRepository  _cfRepo      = Substitute.For<IClienteFinalRepository>();
    private readonly IAplicacaoRepository     _appRepo     = Substitute.For<IAplicacaoRepository>();
    private readonly IUnitOfWork              _uow         = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser             _currentUser = Substitute.For<ICurrentUser>();

    // EmitirTokenLicencaHandler é sealed — para testes sem token, passamos null
    // (o handler só chama quando EmitirToken=true, ignorado nos testes de fluxo principal)
    private readonly ILicencaRepository       _licencaTokenRepo = Substitute.For<ILicencaRepository>();
    private readonly ILicencaTokenRepository  _tokenRepo        = Substitute.For<ILicencaTokenRepository>();
    private readonly IHmacLicencaTokenService _hmac             = Substitute.For<IHmacLicencaTokenService>();

    private EmitirTokenLicencaHandler CriarTokenHandler() =>
        new(_licencaTokenRepo, _tokenRepo, _hmac, _uow);

    private EmitirLicencaHandler CriarHandler() =>
        new(_licencaRepo, _cfRepo, _appRepo, _uow, _currentUser, CriarTokenHandler());

    private void ConfigurarTenantValido(Guid idTipoLicenca = default)
    {
        var tipoId = idTipoLicenca == default ? TipoPermanente : idTipoLicenca;
        _currentUser.IdCliente.Returns(IdTenant);

        _cfRepo.BuscarPorIdAsync(IdClienteFinal, Arg.Any<CancellationToken>())
            .Returns(new ClienteFinalResult(IdClienteFinal, IdTenant,
                "CF Teste", 2, "11222333000181", "cf@teste.com", null, true));

        _appRepo.BuscarPorIdAsync(IdAplicativo, Arg.Any<CancellationToken>())
            .Returns(new AplicacaoResult(IdAplicativo, IdTenant,
                "App Teste", null, tipoId, "Permanente", true));

        _licencaRepo.ExisteLicencaAtivaAsync(IdTenant, IdClienteFinal, IdAplicativo, Arg.Any<CancellationToken>())
            .Returns(false);

        _licencaRepo.InserirLicencaAsync(Arg.Any<Domain.Entities.Licenca>(), Arg.Any<CancellationToken>())
            .Returns(x => ((Domain.Entities.Licenca)x[0]).Id);

        _licencaRepo.BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new LicencaResult(
                Guid.NewGuid(), IdTenant, IdClienteFinal, "CF Teste",
                IdAplicativo, "App Teste",
                tipoId, "Permanente", DateTime.UtcNow, true, null, null, null, null, null, null));
    }

    private static EmitirLicencaCommand CommandPermanente() =>
        new(IdClienteFinal, IdAplicativo, null, null, null);

    private static EmitirLicencaCommand CommandPeriodo() =>
        new(IdClienteFinal, IdAplicativo,
            new DetalhePeriodoCommand(DateTime.UtcNow.Date, DateTime.UtcNow.AddYears(1)),
            null, null);

    // -------------------------------------------------------------------------
    // Validação de entrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_DadosInvalidos_RetornaInvalido()
    {
        var cmd = new EmitirLicencaCommand(Guid.Empty, IdAplicativo, null, null, null);

        var resultado = await CriarHandler().HandleAsync(cmd);

        resultado.Should().BeOfType<EmitirLicencaResult.Invalido>();
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ClienteFinalNaoEncontrado_RetornaClienteFinalNaoEncontrado()
    {
        _currentUser.IdCliente.Returns(IdTenant);
        _cfRepo.BuscarPorIdAsync(IdClienteFinal, Arg.Any<CancellationToken>())
            .Returns((ClienteFinalResult?)null);

        var resultado = await CriarHandler().HandleAsync(CommandPermanente());

        resultado.Should().BeOfType<EmitirLicencaResult.ClienteFinalNaoEncontrado>();
    }

    [Fact]
    public async Task Handle_ClienteFinalDeOutroTenant_RetornaAcessoNegado()
    {
        _currentUser.IdCliente.Returns(IdTenant);
        _cfRepo.BuscarPorIdAsync(IdClienteFinal, Arg.Any<CancellationToken>())
            .Returns(new ClienteFinalResult(IdClienteFinal, Guid.NewGuid(),
                "Outro", 2, "11222333000181", "x@x.com", null, true));

        var resultado = await CriarHandler().HandleAsync(CommandPermanente());

        resultado.Should().BeOfType<EmitirLicencaResult.AcessoNegado>();
    }

    [Fact]
    public async Task Handle_AplicacaoDeOutroTenant_RetornaAcessoNegado()
    {
        _currentUser.IdCliente.Returns(IdTenant);
        _cfRepo.BuscarPorIdAsync(IdClienteFinal, Arg.Any<CancellationToken>())
            .Returns(new ClienteFinalResult(IdClienteFinal, IdTenant,
                "CF", 2, "11222333000181", "cf@x.com", null, true));
        _appRepo.BuscarPorIdAsync(IdAplicativo, Arg.Any<CancellationToken>())
            .Returns(new AplicacaoResult(IdAplicativo, Guid.NewGuid(),
                "App", null, TipoPermanente, "Permanente", true));

        var resultado = await CriarHandler().HandleAsync(CommandPermanente());

        resultado.Should().BeOfType<EmitirLicencaResult.AcessoNegado>();
    }

    // -------------------------------------------------------------------------
    // Compatibilidade de detalhe
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_PeriodoParaTipoPermanente_RetornaTipoIncompativel()
    {
        ConfigurarTenantValido(TipoPermanente);

        var cmd = new EmitirLicencaCommand(IdClienteFinal, IdAplicativo,
            new DetalhePeriodoCommand(DateTime.UtcNow.Date, DateTime.UtcNow.AddYears(1)),
            null, null);

        var resultado = await CriarHandler().HandleAsync(cmd);

        resultado.Should().BeOfType<EmitirLicencaResult.TipoLicencaIncompativel>();
    }

    [Fact]
    public async Task Handle_SemDetalheParaTipoPeriodo_RetornaTipoIncompativel()
    {
        ConfigurarTenantValido(TipoPeriodo);

        var resultado = await CriarHandler().HandleAsync(CommandPermanente());

        resultado.Should().BeOfType<EmitirLicencaResult.TipoLicencaIncompativel>();
    }

    // -------------------------------------------------------------------------
    // Duplicata
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaDuplicada_RetornaLicencaDuplicada()
    {
        ConfigurarTenantValido(TipoPermanente);
        _licencaRepo.ExisteLicencaAtivaAsync(IdTenant, IdClienteFinal, IdAplicativo, Arg.Any<CancellationToken>())
            .Returns(true);

        var resultado = await CriarHandler().HandleAsync(CommandPermanente());

        resultado.Should().BeOfType<EmitirLicencaResult.LicencaDuplicada>();
    }

    // -------------------------------------------------------------------------
    // Sucesso
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaPermanenteSemToken_RetornaSucessoTokenNulo()
    {
        ConfigurarTenantValido(TipoPermanente);

        var resultado = await CriarHandler().HandleAsync(CommandPermanente());

        resultado.Should().BeOfType<EmitirLicencaResult.Sucesso>();
        var sucesso = (EmitirLicencaResult.Sucesso)resultado;
        sucesso.TokenTexto.Should().BeNull();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LicencaPeriodoValida_RetornaSucesso()
    {
        ConfigurarTenantValido(TipoPeriodo);

        _licencaRepo.BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new LicencaResult(
                Guid.NewGuid(), IdTenant, IdClienteFinal, "CF Teste",
                IdAplicativo, "App Teste",
                TipoPeriodo, "Por Período", DateTime.UtcNow, true,
                new DetalhePeriodoResult(DateTime.UtcNow.Date, DateTime.UtcNow.AddYears(1), false),
                null, null, null, null, null));

        var resultado = await CriarHandler().HandleAsync(CommandPeriodo());

        resultado.Should().BeOfType<EmitirLicencaResult.Sucesso>();
        var sucesso = (EmitirLicencaResult.Sucesso)resultado;
        sucesso.Licenca.Periodo.Should().NotBeNull();
    }
}
