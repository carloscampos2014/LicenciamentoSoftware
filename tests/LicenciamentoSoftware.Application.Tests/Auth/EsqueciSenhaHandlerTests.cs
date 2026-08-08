using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class EsqueciSenhaHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IRecuperacaoSenhaRepository _recuperacaoRepo = Substitute.For<IRecuperacaoSenhaRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _renderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly EsqueciSenhaHandler _handler;

    private static readonly DateTime Agora = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    public EsqueciSenhaHandlerTests()
    {
        _clock.UtcNow.Returns(Agora);
        _renderer.Renderizar(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>link</html>");

        _handler = new EsqueciSenhaHandler(
            _usuarioRepo, _recuperacaoRepo, _email, _renderer,
            _clock, "https://licensemanager.enzojb.com.br");
    }

    [Fact]
    public async Task HandleAsync_EmailExistente_EnviaEmailESalvaToken()
    {
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _usuarioRepo.BuscarPorEmailAsync("carlos@email.com", default).Returns(usuario);

        await _handler.HandleAsync("carlos@email.com");

        await _recuperacaoRepo.Received(1)
            .SalvarAsync(usuario.Id, Arg.Any<string>(), Arg.Any<DateTime>(), default);
        await _email.Received(1)
            .EnviarAsync("carlos@email.com", Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailNaoExistente_NaoEnviaEmailNemSalvaToken()
    {
        _usuarioRepo.BuscarPorEmailAsync("nao@existe.com", default).Returns((DomainUsuario?)null);

        await _handler.Invoking(h => h.HandleAsync("nao@existe.com"))
            .Should().NotThrowAsync();

        await _recuperacaoRepo.DidNotReceive()
            .SalvarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), default);
        await _email.DidNotReceive()
            .EnviarAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TokenExpiracaoEm1Hora()
    {
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _usuarioRepo.BuscarPorEmailAsync("carlos@email.com", default).Returns(usuario);

        await _handler.HandleAsync("carlos@email.com");

        var esperado = Agora.AddHours(1);
        await _recuperacaoRepo.Received(1)
            .SalvarAsync(usuario.Id, Arg.Any<string>(),
                Arg.Is<DateTime>(d => d == esperado), default);
    }
}
