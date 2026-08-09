using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class ConfirmarReset2FAHandlerTests
{
    private readonly ISolicitacaoReset2FARepository _repo = Substitute.For<ISolicitacaoReset2FARepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ConfirmarReset2FAHandler _handler;

    private static readonly DateTime Agora = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    public ConfirmarReset2FAHandlerTests()
    {
        _clock.UtcNow.Returns(Agora);
        _handler = new ConfirmarReset2FAHandler(_repo, _uow, _clock);
    }

    private static string HashToken(string token) =>
        System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    [Fact]
    public async Task HandleAsync_TokenValido_RetornaSucesso()
    {
        var tokenBruto = "abc123def456";
        var registro = new TokenConfirmacaoReset(Guid.NewGuid(), Guid.NewGuid(),
            Agora.AddMinutes(10), "127.0.0.1");
        var idSolicitacao = Guid.NewGuid();

        _repo.BuscarTokenAsync(HashToken(tokenBruto), default).Returns(registro);
        _repo.ConfirmarECriarSolicitacaoAsync(registro.Id, default).Returns(idSolicitacao);

        var result = await _handler.HandleAsync(tokenBruto);

        result.Should().BeOfType<ConfirmarReset2FAResult.Sucesso>()
            .Which.IdSolicitacao.Should().Be(idSolicitacao);
        await _uow.Received(1).CommitAsync(default);
    }

    [Fact]
    public async Task HandleAsync_TokenNaoEncontrado_RetornaTokenInvalido()
    {
        _repo.BuscarTokenAsync(Arg.Any<string>(), default)
            .Returns((TokenConfirmacaoReset?)null);

        var result = await _handler.HandleAsync("token-invalido");

        result.Should().BeOfType<ConfirmarReset2FAResult.TokenInvalidoOuExpirado>();
        await _repo.DidNotReceive().ConfirmarECriarSolicitacaoAsync(Arg.Any<Guid>(), default);
    }

    [Fact]
    public async Task HandleAsync_TokenExpirado_RetornaTokenInvalido()
    {
        var tokenExpirado = new TokenConfirmacaoReset(Guid.NewGuid(), Guid.NewGuid(),
            Agora.AddMinutes(-1), null);
        _repo.BuscarTokenAsync(Arg.Any<string>(), default).Returns(tokenExpirado);

        var result = await _handler.HandleAsync("token-expirado");

        result.Should().BeOfType<ConfirmarReset2FAResult.TokenInvalidoOuExpirado>();
    }
}
