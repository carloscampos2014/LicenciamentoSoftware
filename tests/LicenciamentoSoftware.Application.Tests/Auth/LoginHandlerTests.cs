using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Auth.Results;
using LicenciamentoSoftware.Domain.Entities;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Auth;

public class LoginHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private LoginHandler CriarHandler() =>
        new(_usuarioRepo, _hasher, _jwt, _refreshRepo, _clock);

    private static Usuario CriarUsuario(bool comTotp = false)
    {
        var u = Usuario.Criar(Guid.NewGuid(), "Teste", "hash_bcrypt");
        if (comTotp) u.DefinirTotpSecret("SEGREDO_TOTP_BASE32");
        return u;
    }

    [Fact]
    public async Task Login_UsuarioNaoEncontrado_RetornaNegado()
    {
        _usuarioRepo.BuscarPorEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);

        var resultado = await CriarHandler().HandleAsync(
            new LoginCommand("teste@email.com", "senha123"));

        resultado.Should().BeOfType<AuthResult.Negado>();
    }

    [Fact]
    public async Task Login_UsuarioInativo_RetornaNegado()
    {
        var usuario = CriarUsuario();
        usuario.Desativar();
        _usuarioRepo.BuscarPorEmailAsync(Arg.Any<string>()).Returns(usuario);

        var resultado = await CriarHandler().HandleAsync(
            new LoginCommand("teste@email.com", "senha123"));

        resultado.Should().BeOfType<AuthResult.Negado>();
    }

    [Fact]
    public async Task Login_SenhaErrada_RetornaNegado()
    {
        var usuario = CriarUsuario();
        _usuarioRepo.BuscarPorEmailAsync(Arg.Any<string>()).Returns(usuario);
        _hasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var resultado = await CriarHandler().HandleAsync(
            new LoginCommand("teste@email.com", "senha_errada"));

        resultado.Should().BeOfType<AuthResult.Negado>();
    }

    [Fact]
    public async Task Login_SemTotp_SenhaCorreta_RetornaSucesso()
    {
        var usuario = CriarUsuario();
        _usuarioRepo.BuscarPorEmailAsync(Arg.Any<string>()).Returns(usuario);
        _hasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _usuarioRepo.BuscarPapelAsync(Arg.Any<Guid>()).Returns("AdministradorCliente");
        _clock.UtcNow.Returns(DateTime.UtcNow);

        var tokenPar = new TokenPar("access", "refresh", DateTime.UtcNow.AddHours(1));
        _jwt.GerarTokenPar(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(tokenPar);

        var resultado = await CriarHandler().HandleAsync(
            new LoginCommand("teste@email.com", "senha_correta"));

        resultado.Should().BeOfType<AuthResult.Sucesso>();
        var sucesso = (AuthResult.Sucesso)resultado;
        sucesso.AccessToken.Should().Be("access");
    }

    [Fact]
    public async Task Login_ComTotp_SenhaCorreta_RetornaRequer2FA()
    {
        var usuario = CriarUsuario(comTotp: true);
        _usuarioRepo.BuscarPorEmailAsync(Arg.Any<string>()).Returns(usuario);
        _hasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var tokenPar = new TokenPar("token_temporario", "refresh", DateTime.UtcNow.AddMinutes(5));
        _jwt.GerarTokenPar(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(tokenPar);

        var resultado = await CriarHandler().HandleAsync(
            new LoginCommand("teste@email.com", "senha_correta"));

        resultado.Should().BeOfType<AuthResult.Requer2FA>();
        var desafio = (AuthResult.Requer2FA)resultado;
        desafio.TokenTemporario.Should().Be("token_temporario");
    }
}
