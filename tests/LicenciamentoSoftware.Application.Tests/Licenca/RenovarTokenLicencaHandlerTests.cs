using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class RenovarTokenLicencaHandlerTests
{
    private readonly ILicencaRepository _licencaRepo = Substitute.For<ILicencaRepository>();
    private readonly ILicencaTokenRepository _tokenRepo = Substitute.For<ILicencaTokenRepository>();
    private readonly IHmacLicencaTokenService _hmac = Substitute.For<IHmacLicencaTokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid IdLicenca = Guid.NewGuid();

    private RenovarTokenLicencaHandler CriarHandler(int defaultExpiracao = 525600) =>
        new(_licencaRepo, _tokenRepo, _hmac, _uow, defaultExpiracao);

    private static LicencaInfo LicencaAtiva() =>
        new(IdLicenca, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Ativo: true);

    private static LicencaTokenInfo TokenAtivo() =>
        new(Guid.NewGuid(), IdLicenca, "$2a$12$hash_antigo", 525600, DateTime.UtcNow.AddDays(-1), true);

    // -------------------------------------------------------------------------
    // Cenários de falha
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_LicencaNaoEncontrada_RetornaLicencaNaoEncontrada()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns((LicencaInfo?)null);

        var resultado = await CriarHandler().HandleAsync(
            new RenovarTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.LicencaNaoEncontrada>();
    }

    [Fact]
    public async Task Handle_LicencaInativa_RetornaLicencaInativa()
    {
        var licencaInativa = new LicencaInfo(IdLicenca, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Ativo: false);
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(licencaInativa);

        var resultado = await CriarHandler().HandleAsync(
            new RenovarTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.LicencaInativa>();
    }

    // -------------------------------------------------------------------------
    // Cenário: token existente — deve atualizar (não criar novo)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_TokenExistente_AtualizaEmVezDeCriarNovo()
    {
        var tokenExistente = TokenAtivo();
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns(tokenExistente);
        _hmac.GerarSegredo().Returns("novo-segredo");
        _hmac.HashSegredo("novo-segredo").Returns("$2a$12$novo_hash");

        var resultado = await CriarHandler().HandleAsync(
            new RenovarTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.Sucesso>();
        var sucesso = (EmitirTokenResult.Sucesso)resultado;
        sucesso.IdToken.Should().Be(tokenExistente.Id);
        sucesso.TokenTexto.Should().Be("novo-segredo");

        await _tokenRepo.Received(1).AtualizarAsync(
            tokenExistente.Id, "$2a$12$novo_hash", 525600,
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _tokenRepo.DidNotReceive().SalvarAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Cenário: sem token ativo — cria novo (comportamento idempotente)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SemTokenAtivo_CriaNovoToken()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns((LicencaTokenInfo?)null);
        _hmac.GerarSegredo().Returns("segredo-novo");
        _hmac.HashSegredo("segredo-novo").Returns("$2a$12$hash_novo");

        var resultado = await CriarHandler().HandleAsync(
            new RenovarTokenLicencaCommand(IdLicenca));

        resultado.Should().BeOfType<EmitirTokenResult.Sucesso>();

        await _tokenRepo.Received(1).SalvarAsync(
            Arg.Any<Guid>(), IdLicenca, "$2a$12$hash_novo", 525600,
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _tokenRepo.DidNotReceive().AtualizarAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComExpiracaoOverride_UsaValorInformado()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns(TokenAtivo());
        _hmac.GerarSegredo().Returns("segredo");
        _hmac.HashSegredo("segredo").Returns("hash");

        var resultado = await CriarHandler().HandleAsync(
            new RenovarTokenLicencaCommand(IdLicenca, ExpiracaoMinutosOverride: 2880));

        var sucesso = resultado.Should().BeOfType<EmitirTokenResult.Sucesso>().Subject;
        sucesso.ExpiracaoMinutos.Should().Be(2880);
    }

    [Fact]
    public async Task Handle_Sucesso_ExecutaDentroDeUow()
    {
        _licencaRepo.BuscarPorIdAsync(IdLicenca).Returns(LicencaAtiva());
        _tokenRepo.BuscarAtivoporLicencaAsync(IdLicenca).Returns(TokenAtivo());
        _hmac.GerarSegredo().Returns("segredo");
        _hmac.HashSegredo("segredo").Returns("hash");

        await CriarHandler().HandleAsync(new RenovarTokenLicencaCommand(IdLicenca));

        await _uow.Received(1).BeginAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
