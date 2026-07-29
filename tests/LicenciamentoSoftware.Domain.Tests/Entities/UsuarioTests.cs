using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class UsuarioTests
{
    private static readonly Guid ClienteId = Guid.NewGuid();
    private const string SenhaHashValida = "hash_bcrypt_simulado";

    [Fact]
    public void Criar_DadosValidos_RetornaUsuarioAtivo()
    {
        var usuario = Usuario.Criar(ClienteId, "João Silva", SenhaHashValida);

        usuario.Id.Should().NotBe(Guid.Empty);
        usuario.Nome.Should().Be("João Silva");
        usuario.SenhaHash.Should().Be(SenhaHashValida);
        usuario.TotpSecretHash.Should().BeNull();
        usuario.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Criar_IdClienteVazio_LancaDomainException()
    {
        var act = () => Usuario.Criar(Guid.Empty, "João", SenhaHashValida);
        act.Should().Throw<DomainException>().WithMessage("*IdCliente*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Criar_NomeVazio_LancaDomainException(string? nome)
    {
        var act = () => Usuario.Criar(ClienteId, nome!, SenhaHashValida);
        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Criar_SenhaHashVazia_LancaDomainException(string? hash)
    {
        var act = () => Usuario.Criar(ClienteId, "João", hash!);
        act.Should().Throw<DomainException>().WithMessage("*senha*");
    }

    [Fact]
    public void DefinirTotpSecret_ValorValido_ArmazenaSecret()
    {
        var usuario = Usuario.Criar(ClienteId, "João", SenhaHashValida);
        usuario.DefinirTotpSecret("totp_hash_secreto");
        usuario.TotpSecretHash.Should().Be("totp_hash_secreto");
    }

    [Fact]
    public void RemoverTotpSecret_ComSecret_TornaNull()
    {
        var usuario = Usuario.Criar(ClienteId, "João", SenhaHashValida);
        usuario.DefinirTotpSecret("totp_hash_secreto");
        usuario.RemoverTotpSecret();
        usuario.TotpSecretHash.Should().BeNull();
    }

    [Fact]
    public void Desativar_UsuarioAtivo_TornaInativo()
    {
        var usuario = Usuario.Criar(ClienteId, "João", SenhaHashValida);
        usuario.Desativar();
        usuario.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_UsuarioJaInativo_LancaDomainException()
    {
        var usuario = Usuario.Criar(ClienteId, "João", SenhaHashValida);
        usuario.Desativar();

        var act = () => usuario.Desativar();
        act.Should().Throw<DomainException>().WithMessage("*inativo*");
    }
}
