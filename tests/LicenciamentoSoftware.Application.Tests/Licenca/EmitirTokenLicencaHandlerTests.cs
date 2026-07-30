using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class EmitirTokenLicencaHandlerTests
{
    private readonly ILicencaRepository _licencaRepo = Substitute.For<ILicencaRepository>();
    private readonly ILicencaTokenRepository _tokenRepo = Substitute.For<ILicencaTokenRepository>();
    private readonly IHmacLicencaTokenService _hmac = Substitute.For<IHmacLicencaTokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid IdLicenca = Guid.NewGuid();

    private EmitirTokenLicencaHandler CriarHandler(int defaultExpiracao = 525600) =>
        new(_licencaRepo, _tokenRepo, _hmac, _uow, defaultExpiracao);

    private static LicencaInfo LicencaAtiva() =>
        new(IdLicenca, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Ativo: true);

    // -------------------------------------------------------------------------
    // Cenários de falha
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaNaoEncontrada_RetornaLicencaNaoEncontrada()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns((LicencaInfo?)null);

        var resultado = await CriarHandler().HandleAsync(
            new EmitirTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.LicencaNaoEncontrada>();
    }

    [Fact]
    public async Task Handle_LicencaInativa_RetornaLicencaInativa()
    {
        var licencaInativa = new LicencaInfo(IdLicenca, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Ativo: false);
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(licencaInativa);

        var resultado = await CriarHandler().HandleAsync(
            new EmitirTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.LicencaInativa>();
    }

    [Fact]
    public async Task Handle_TokenJaExiste_RetornaTokenJaExiste()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        var tokenExistente = new LicencaTokenInfo(Guid.NewGuid(), IdLicenca, "hash", 60, DateTime.UtcNow, true);
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns(tokenExistente);

        var resultado = await CriarHandler().HandleAsync(
            new EmitirTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.TokenJaExiste>();
    }

    // -------------------------------------------------------------------------
    // Cenário de sucesso
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaAtivaSeToken_EmiteTokenComSucesso()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns((LicencaTokenInfo?)null);
        _hmac.GerarSegredo().Returns("segredo-texto-puro-base64");
        _hmac.HashSegredo("segredo-texto-puro-base64").Returns("$2a$12$hash");

        var resultado = await CriarHandler().HandleAsync(
            new EmitirTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.Sucesso>();
        var sucesso = (EmitirTokenResult.Sucesso)resultado;
        sucesso.IdLicenca.Should().Be(IdLicenca);
        sucesso.TokenTexto.Should().Be("segredo-texto-puro-base64");
        sucesso.ExpiracaoMinutos.Should().Be(525600);
    }

    [Fact]
    public async Task Handle_ComExpiracaoOverride_UsaValorInformado()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns((LicencaTokenInfo?)null);
        _hmac.GerarSegredo().Returns("segredo");
        _hmac.HashSegredo("segredo").Returns("hash");

        var resultado = await CriarHandler().HandleAsync(
            new EmitirTokenLicencaCommand(IdLicenca, ExpiracaoMinutosOverride: 1440));

        var sucesso = resultado.Should().BeOfType<EmitirTokenResult.Sucesso>().Subject;
        sucesso.ExpiracaoMinutos.Should().Be(1440);
    }

    [Fact]
    public async Task Handle_Sucesso_PersisteDentroDeUow()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns((LicencaTokenInfo?)null);
        _hmac.GerarSegredo().Returns("segredo");
        _hmac.HashSegredo("segredo").Returns("hash");

        await CriarHandler().HandleAsync(new EmitirTokenLicencaCommand(IdLicenca));

        await _uow.Received(1).BeginAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _tokenRepo.Received(1).SalvarAsync(
            Arg.Any<Guid>(), IdLicenca, "hash", 525600, Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
