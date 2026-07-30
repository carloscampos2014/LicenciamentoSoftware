using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Auth.Results;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class VerificarTotpHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly ITotpService _totp = Substitute.For<ITotpService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private VerificarTotpHandler CriarHandler() =>
        new(_usuarioRepo, _totp, _jwt, _refreshRepo, _clock);

    [Fact]
    public async Task VerificarTotp_TokenDesafioInvalido_RetornaTotpInvalido()
    {
        _jwt.ValidarAccessToken(Arg.Any<string>(), out Arg.Any<Guid>())
            .Returns(false);

        var resultado = await CriarHandler().HandleAsync(
            new VerificarTotpCommand("token_invalido", "123456"));

        resultado.Should().BeOfType<AuthResult.TotpInvalido>();
    }

    [Fact]
    public async Task VerificarTotp_CodigoTotpInvalido_RetornaTotpInvalido()
    {
        var idUsuario = Guid.NewGuid();
        _jwt.ValidarAccessToken(Arg.Any<string>(), out Arg.Any<Guid>())
            .Returns(x => { x[1] = idUsuario; return true; });

        var usuario = DomainUsuario.Criar(idUsuario, "Teste", "hash");
        usuario.DefinirTotpSecret("SEGREDO");
        _usuarioRepo.BuscarPorIdAsync(idUsuario).Returns(usuario);
        _totp.Validar(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var resultado = await CriarHandler().HandleAsync(
            new VerificarTotpCommand("token_valido", "000000"));

        resultado.Should().BeOfType<AuthResult.TotpInvalido>();
    }

    [Fact]
    public async Task VerificarTotp_CodigoValido_RetornaSucesso()
    {
        var idUsuario = Guid.NewGuid();
        _jwt.ValidarAccessToken(Arg.Any<string>(), out Arg.Any<Guid>())
            .Returns(x => { x[1] = idUsuario; return true; });

        var usuario = DomainUsuario.Criar(idUsuario, "Teste", "hash");
        usuario.DefinirTotpSecret("SEGREDO");
        _usuarioRepo.BuscarPorIdAsync(idUsuario).Returns(usuario);
        _totp.Validar(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _usuarioRepo.BuscarPapelAsync(idUsuario).Returns("OperadorCliente");
        _clock.UtcNow.Returns(DateTime.UtcNow);

        var tokenPar = new TokenPar("access_completo", "refresh", DateTime.UtcNow.AddHours(1));
        _jwt.GerarTokenPar(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(tokenPar);

        var resultado = await CriarHandler().HandleAsync(
            new VerificarTotpCommand("token_valido", "123456"));

        resultado.Should().BeOfType<AuthResult.Sucesso>();
        var sucesso = (AuthResult.Sucesso)resultado;
        sucesso.AccessToken.Should().Be("access_completo");
    }
}
