using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class LicencaTokenTests
{
    private static readonly Guid LicencaId = Guid.NewGuid();
    private const string HashValido = "$2a$12$hash_simulado_bcrypt_valido";

    // -------------------------------------------------------------------------
    // Criar
    // -------------------------------------------------------------------------

    [Fact]
    public void Criar_DadosValidos_RetornaTokenAtivo()
    {
        var token = LicencaToken.Criar(LicencaId, HashValido, 60);

        token.Id.Should().NotBe(Guid.Empty);
        token.IdLicenca.Should().Be(LicencaId);
        token.SegredoHash.Should().Be(HashValido);
        token.ExpiracaoMinutos.Should().Be(60);
        token.Ativo.Should().BeTrue();
        token.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Criar_IdLicencaVazio_LancaDomainException()
    {
        var act = () => LicencaToken.Criar(Guid.Empty, HashValido, 60);
        act.Should().Throw<DomainException>().WithMessage("*IdLicenca*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Criar_SegredoHashVazio_LancaDomainException(string? hash)
    {
        var act = () => LicencaToken.Criar(LicencaId, hash!, 60);
        act.Should().Throw<DomainException>().WithMessage("*SegredoHash*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Criar_ExpiracaoMenorOuIgualZero_LancaDomainException(int expiracao)
    {
        var act = () => LicencaToken.Criar(LicencaId, HashValido, expiracao);
        act.Should().Throw<DomainException>().WithMessage("*ExpiracaoMinutos*");
    }

    // -------------------------------------------------------------------------
    // Revogar
    // -------------------------------------------------------------------------

    [Fact]
    public void Revogar_TokenAtivo_TornaInativo()
    {
        var token = LicencaToken.Criar(LicencaId, HashValido, 60);

        token.Revogar();

        token.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Revogar_TokenJaRevogado_LancaDomainException()
    {
        var token = LicencaToken.Criar(LicencaId, HashValido, 60);
        token.Revogar();

        var act = () => token.Revogar();
        act.Should().Throw<DomainException>().WithMessage("*revogado*");
    }

    // -------------------------------------------------------------------------
    // Renovar
    // -------------------------------------------------------------------------

    [Fact]
    public void Renovar_DadosValidos_AtualizaHashEExpiracao()
    {
        var token = LicencaToken.Criar(LicencaId, HashValido, 60);
        const string novoHash = "$2a$12$novo_hash_bcrypt";

        token.Renovar(novoHash, 120);

        token.SegredoHash.Should().Be(novoHash);
        token.ExpiracaoMinutos.Should().Be(120);
        token.Ativo.Should().BeTrue();
        token.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Renovar_NovoHashVazio_LancaDomainException(string? hash)
    {
        var token = LicencaToken.Criar(LicencaId, HashValido, 60);

        var act = () => token.Renovar(hash!, 60);
        act.Should().Throw<DomainException>().WithMessage("*NovoSegredoHash*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Renovar_ExpiracaoInvalida_LancaDomainException(int expiracao)
    {
        var token = LicencaToken.Criar(LicencaId, HashValido, 60);

        var act = () => token.Renovar(HashValido, expiracao);
        act.Should().Throw<DomainException>().WithMessage("*ExpiracaoMinutos*");
    }
}
