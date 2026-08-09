using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class AprovarReset2FAHandlerTests
{
    private readonly ISolicitacaoReset2FARepository _repo = Substitute.For<ISolicitacaoReset2FARepository>();
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _renderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly AprovarReset2FAHandler _handler;

    public AprovarReset2FAHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc));
        _renderer.Renderizar(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>ok</html>");
        _handler = new AprovarReset2FAHandler(_repo, _usuarioRepo, _email, _renderer, _uow, _clock);
    }

    [Fact]
    public async Task HandleAsync_SolicitacaoPendente_AprovaSucesso()
    {
        // Usar o mesmo idUsuario na solicitacao e no usuario
        var idUsuario = Guid.NewGuid();
        var idSolicitacao = Guid.NewGuid();
        var solicitacao = new SolicitacaoReset2FAInfo(
            idSolicitacao, idUsuario, "Carlos", "carlos@email.com",
            "Empresa X", "Pendente", "127.0.0.1",
            new DateTime(2026, 8, 8, 11, 0, 0, DateTimeKind.Utc));
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");

        _repo.BuscarPorIdAsync(idSolicitacao, default).Returns(solicitacao);
        _usuarioRepo.BuscarPorIdAsync(idUsuario, default).Returns(usuario);

        var result = await _handler.HandleAsync(idSolicitacao);

        result.Should().BeOfType<AprovarReset2FAResult.Sucesso>();
        // O handler chama AtualizarTotpSecretAsync com o Id do usuario retornado pelo repo
        await _usuarioRepo.Received(1)
            .AtualizarTotpSecretAsync(usuario.Id, null, default);
        await _repo.Received(1).AprovarAsync(idSolicitacao, default);
        await _uow.Received(1).CommitAsync(default);
    }

    [Fact]
    public async Task HandleAsync_SolicitacaoNaoEncontrada_RetornaErro()
    {
        _repo.BuscarPorIdAsync(Arg.Any<Guid>(), default)
            .Returns((SolicitacaoReset2FAInfo?)null);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result.Should().BeOfType<AprovarReset2FAResult.SolicitacaoNaoEncontrada>();
        await _usuarioRepo.DidNotReceive()
            .AtualizarTotpSecretAsync(Arg.Any<Guid>(), Arg.Any<string?>(), default);
    }

    [Fact]
    public async Task HandleAsync_SolicitacaoJaProcessada_RetornaJaProcessada()
    {
        var idSolicitacao = Guid.NewGuid();
        var solicitacaoAprovada = new SolicitacaoReset2FAInfo(
            idSolicitacao, Guid.NewGuid(), "Carlos", "carlos@email.com",
            "Empresa X", "Aprovado", null,
            new DateTime(2026, 8, 8, 11, 0, 0, DateTimeKind.Utc));

        _repo.BuscarPorIdAsync(idSolicitacao, default).Returns(solicitacaoAprovada);

        var result = await _handler.HandleAsync(idSolicitacao);

        result.Should().BeOfType<AprovarReset2FAResult.JaProcessada>();
    }
}
