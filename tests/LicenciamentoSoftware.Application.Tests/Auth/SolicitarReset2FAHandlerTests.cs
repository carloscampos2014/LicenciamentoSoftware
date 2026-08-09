using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class SolicitarReset2FAHandlerTests
{
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ISolicitacaoReset2FARepository _repo = Substitute.For<ISolicitacaoReset2FARepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _renderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly SolicitarReset2FAHandler _handler;

    public SolicitarReset2FAHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc));
        _renderer.Renderizar(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>ok</html>");
        _handler = new SolicitarReset2FAHandler(
            _jwt, _usuarioRepo, _hasher, _repo, _email, _renderer,
            _clock, "https://licensemanager.enzojb.com.br");
    }

    [Fact]
    public async Task HandleAsync_TokenValidoSenhaCorreta_Envia()
    {
        var idUsuario = Guid.NewGuid();
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _jwt.ValidarAccessToken("token-temp", out Arg.Any<Guid>())
            .Returns(x => { x[1] = idUsuario; return true; });
        _usuarioRepo.BuscarPorIdAsync(idUsuario, default).Returns(usuario);
        _hasher.Verificar("senha123", usuario.SenhaHash).Returns(true);

        var cmd = new SolicitarReset2FACommand("token-temp", "senha123", "127.0.0.1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<SolicitarReset2FAResult.Enviado>();
        await _repo.Received(1).SalvarTokenAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), "127.0.0.1", default);
        await _email.Received(1).EnviarAsync(
            "carlos@email.com", Arg.Any<string>(), Arg.Any<string>(), default);
    }

    [Fact]
    public async Task HandleAsync_TokenLoginInvalido_RetornaTokenInvalido()
    {
        _jwt.ValidarAccessToken("invalido", out Arg.Any<Guid>()).Returns(false);

        var cmd = new SolicitarReset2FACommand("invalido", "senha123", null);
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<SolicitarReset2FAResult.TokenLoginInvalido>();
        await _email.DidNotReceive().EnviarAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), default);
    }

    [Fact]
    public async Task HandleAsync_SenhaIncorreta_RetornaSenhaIncorreta()
    {
        var idUsuario = Guid.NewGuid();
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _jwt.ValidarAccessToken("token-temp", out Arg.Any<Guid>())
            .Returns(x => { x[1] = idUsuario; return true; });
        _usuarioRepo.BuscarPorIdAsync(idUsuario, default).Returns(usuario);
        _hasher.Verificar("errada", usuario.SenhaHash).Returns(false);

        var cmd = new SolicitarReset2FACommand("token-temp", "errada", null);
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<SolicitarReset2FAResult.SenhaIncorreta>();
    }
}
