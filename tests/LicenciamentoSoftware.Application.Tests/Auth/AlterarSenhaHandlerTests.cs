using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class AlterarSenhaHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly AlterarSenhaHandler _handler;

    public AlterarSenhaHandlerTests()
    {
        _handler = new AlterarSenhaHandler(_usuarioRepo, _hasher, _uow);
    }

    [Fact]
    public async Task HandleAsync_SenhaCorreta_RetornaSucesso()
    {
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _usuarioRepo.BuscarPorIdAsync(usuario.Id, default).Returns(usuario);
        _hasher.Verificar("senhaAtual", usuario.SenhaHash).Returns(true);
        _hasher.Hash("novaSenha1").Returns("$2a$12$novo");

        var cmd = new AlterarSenhaCommand(usuario.Id, "senhaAtual", "novaSenha1", "novaSenha1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<AlterarSenhaResult.Sucesso>();
        await _usuarioRepo.Received(1).DefinirSenhaAsync(usuario.Id, "$2a$12$novo", default);
        await _usuarioRepo.Received(1).RevogarTodosRefreshTokensAsync(usuario.Id, default);
    }

    [Fact]
    public async Task HandleAsync_SenhaAtualIncorreta_RetornaSenhaAtualIncorreta()
    {
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _usuarioRepo.BuscarPorIdAsync(usuario.Id, default).Returns(usuario);
        _hasher.Verificar("errada", usuario.SenhaHash).Returns(false);

        var cmd = new AlterarSenhaCommand(usuario.Id, "errada", "novaSenha1", "novaSenha1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<AlterarSenhaResult.SenhaAtualIncorreta>();
        await _usuarioRepo.DidNotReceive()
            .DefinirSenhaAsync(Arg.Any<Guid>(), Arg.Any<string>(), default);
    }

    [Fact]
    public async Task HandleAsync_ConfirmacaoNaoConfere_RetornaInvalido()
    {
        var cmd = new AlterarSenhaCommand(Guid.NewGuid(), "senhaAtual", "novaSenha1", "diferente");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<AlterarSenhaResult.Invalido>()
            .Which.Erros.Should().ContainSingle(e => e.Contains("confirmação"));
    }

    [Fact]
    public async Task HandleAsync_NovaSenhaCurta_RetornaInvalido()
    {
        var cmd = new AlterarSenhaCommand(Guid.NewGuid(), "senhaAtual", "1234567", "1234567");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<AlterarSenhaResult.Invalido>()
            .Which.Erros.Should().ContainSingle(e => e.Contains("8 caracteres"));
    }

    [Fact]
    public async Task HandleAsync_UsuarioNaoEncontrado_RetornaUsuarioNaoEncontrado()
    {
        var id = Guid.NewGuid();
        _usuarioRepo.BuscarPorIdAsync(id, default).Returns((DomainUsuario?)null);

        var cmd = new AlterarSenhaCommand(id, "senhaAtual", "novaSenha1", "novaSenha1");
        var result = await _handler.HandleAsync(cmd);

        result.Should().BeOfType<AlterarSenhaResult.UsuarioNaoEncontrado>();
    }
}
