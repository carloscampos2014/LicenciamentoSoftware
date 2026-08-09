using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class RedefinirSenhaHandlerTests
{
    private readonly IRecuperacaoSenhaRepository _recuperacaoRepo = Substitute.For<IRecuperacaoSenhaRepository>();
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly RedefinirSenhaHandler _handler;

    private static readonly DateTime Agora = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    public RedefinirSenhaHandlerTests()
    {
        _clock.UtcNow.Returns(Agora);
        _handler = new RedefinirSenhaHandler(_recuperacaoRepo, _usuarioRepo, _hasher, _uow, _clock);
    }

    private static TokenRecuperacao TokenValido(Guid idUsuario) =>
        new(Guid.NewGuid(), idUsuario, Agora.AddMinutes(30));

    private static string HashToken(string token) =>
        System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    [Fact]
    public async Task HandleAsync_TokenValidoSenhaCorreta_RetornaSucesso()
    {
        var idUsuario = Guid.NewGuid();
        var token = TokenValido(idUsuario);
        _recuperacaoRepo.BuscarPorHashAsync(HashToken("token-bruto"), default).Returns(token);
        _hasher.Hash("novaSenha1").Returns("$2a$12$novo");

        var cmd = new RedefinirSenhaCommand("token-bruto", "novaSenha1", "novaSenha1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<RedefinirSenhaResult.Sucesso>();
        await _recuperacaoRepo.Received(1).MarcarComoUsadoAsync(token.Id, default);
        await _usuarioRepo.Received(1).DefinirSenhaAsync(idUsuario, "$2a$12$novo", default);
        await _usuarioRepo.Received(1).RevogarTodosRefreshTokensAsync(idUsuario, default);
    }

    [Fact]
    public async Task HandleAsync_TokenNaoEncontrado_RetornaTokenInvalido()
    {
        _recuperacaoRepo.BuscarPorHashAsync(Arg.Any<string>(), default)
            .Returns((TokenRecuperacao?)null);

        var cmd = new RedefinirSenhaCommand("token-invalido", "novaSenha1", "novaSenha1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<RedefinirSenhaResult.TokenInvalidoOuExpirado>();
        await _usuarioRepo.DidNotReceive()
            .DefinirSenhaAsync(Arg.Any<Guid>(), Arg.Any<string>(), default);
    }

    [Fact]
    public async Task HandleAsync_TokenExpirado_RetornaTokenInvalido()
    {
        var tokenExpirado = new TokenRecuperacao(Guid.NewGuid(), Guid.NewGuid(), Agora.AddMinutes(-1));
        _recuperacaoRepo.BuscarPorHashAsync(Arg.Any<string>(), default).Returns(tokenExpirado);

        var cmd = new RedefinirSenhaCommand("token-expirado", "novaSenha1", "novaSenha1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<RedefinirSenhaResult.TokenInvalidoOuExpirado>();
    }

    [Fact]
    public async Task HandleAsync_ConfirmacaoNaoConfere_RetornaInvalido()
    {
        var cmd = new RedefinirSenhaCommand("token", "novaSenha1", "diferente");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<RedefinirSenhaResult.Invalido>()
            .Which.Erros.Should().ContainSingle(e => e.Contains("confirmação"));
    }

    [Fact]
    public async Task HandleAsync_SenhaCurta_RetornaInvalido()
    {
        var cmd = new RedefinirSenhaCommand("token", "1234567", "1234567");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<RedefinirSenhaResult.Invalido>()
            .Which.Erros.Should().ContainSingle(e => e.Contains("8 caracteres"));
    }
}
