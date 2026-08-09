using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using NSubstitute;
using DomainUsuario = LicenciamentoSoftware.Domain.Entities.Usuario;
using Xunit;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class ResetarTotpAdminHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ResetarTotpAdminHandler _handler;

    public ResetarTotpAdminHandlerTests()
    {
        _handler = new ResetarTotpAdminHandler(_usuarioRepo, _uow);
    }

    [Fact]
    public async Task HandleAsync_UsuarioExistente_RetornaSucesso()
    {
        var idUsuario = Guid.NewGuid();
        var usuario = DomainUsuario.Criar(Guid.NewGuid(), "Carlos", "$2a$12$hash", "carlos@email.com");
        _usuarioRepo.BuscarPorIdAsync(idUsuario, default).Returns(usuario);

        var result = await _handler.HandleAsync(idUsuario);

        result.Should().BeOfType<ResetarTotpAdminResult.Sucesso>();
        await _usuarioRepo.Received(1).AtualizarTotpSecretAsync(idUsuario, null, default);
        await _uow.Received(1).CommitAsync(default);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNaoEncontrado_RetornaUsuarioNaoEncontrado()
    {
        var idUsuario = Guid.NewGuid();
        _usuarioRepo.BuscarPorIdAsync(idUsuario, default).Returns((DomainUsuario?)null);

        var result = await _handler.HandleAsync(idUsuario);

        result.Should().BeOfType<ResetarTotpAdminResult.UsuarioNaoEncontrado>();
        await _usuarioRepo.DidNotReceive()
            .AtualizarTotpSecretAsync(Arg.Any<Guid>(), Arg.Any<string?>(), default);
    }
}
