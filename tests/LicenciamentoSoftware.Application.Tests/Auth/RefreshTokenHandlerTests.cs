using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Auth.Results;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class RefreshTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private RefreshTokenHandler CriarHandler() =>
        new(_refreshRepo, _usuarioRepo, _jwt, _clock);

    [Fact]
    public async Task Refresh_TokenNaoEncontrado_RetornaTokenInvalido()
    {
        _refreshRepo.BuscarPorHashAsync(Arg.Any<string>()).Returns((RefreshTokenInfo?)null);

        var resultado = await CriarHandler().HandleAsync(
            new RefreshTokenCommand("token_inexistente"));

        resultado.Should().BeOfType<AuthResult.TokenInvalido>();
    }

    [Fact]
    public async Task Refresh_TokenRevogado_RetornaTokenInvalido()
    {
        var info = new RefreshTokenInfo(
            Guid.NewGuid(), Guid.NewGuid(), "hash",
            DateTime.UtcNow.AddDays(30), Revogado: true);

        _refreshRepo.BuscarPorHashAsync(Arg.Any<string>()).Returns(info);

        var resultado = await CriarHandler().HandleAsync(
            new RefreshTokenCommand("token_revogado"));

        resultado.Should().BeOfType<AuthResult.TokenInvalido>();
    }

    [Fact]
    public async Task Refresh_TokenExpirado_RetornaTokenInvalido()
    {
        _clock.UtcNow.Returns(DateTime.UtcNow);

        var info = new RefreshTokenInfo(
            Guid.NewGuid(), Guid.NewGuid(), "hash",
            DateTime.UtcNow.AddDays(-1), Revogado: false);

        _refreshRepo.BuscarPorHashAsync(Arg.Any<string>()).Returns(info);

        var resultado = await CriarHandler().HandleAsync(
            new RefreshTokenCommand("token_expirado"));

        resultado.Should().BeOfType<AuthResult.TokenInvalido>();
    }

    [Fact]
    public async Task Refresh_TokenValido_RotacionaERetornaSucesso()
    {
        var agora = DateTime.UtcNow;
        _clock.UtcNow.Returns(agora);

        var idUsuario = Guid.NewGuid();
        var info = new RefreshTokenInfo(
            Guid.NewGuid(), idUsuario, "hash",
            agora.AddDays(30), Revogado: false);

        _refreshRepo.BuscarPorHashAsync(Arg.Any<string>()).Returns(info);

        var usuario = DomainUsuario.Criar(idUsuario, "Teste", "hash_senha");
        _usuarioRepo.BuscarPorIdAsync(idUsuario).Returns(usuario);
        _usuarioRepo.BuscarPapelAsync(idUsuario).Returns("AdministradorCliente");

        var novoToken = new TokenPar("novo_access", "novo_refresh", agora.AddHours(1));
        _jwt.GerarTokenPar(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(novoToken);

        var resultado = await CriarHandler().HandleAsync(
            new RefreshTokenCommand("token_valido"));

        resultado.Should().BeOfType<AuthResult.Sucesso>();
        var sucesso = (AuthResult.Sucesso)resultado;
        sucesso.AccessToken.Should().Be("novo_access");
        sucesso.RefreshToken.Should().Be("novo_refresh");

        // Garante que o token antigo foi revogado
        await _refreshRepo.Received(1).RevogarAsync(info.Id);
    }
}
