using FluentAssertions;
using LicenciamentoSoftware.Infrastructure.Security;

namespace LicenciamentoSoftware.Application.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_SenhaValida_RetornaHashDiferente()
    {
        var hash = _hasher.Hash("minhasenha123");
        hash.Should().NotBe("minhasenha123");
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verificar_SenhaCorreta_RetornaTrue()
    {
        var hash = _hasher.Hash("senha_correta");
        _hasher.Verificar("senha_correta", hash).Should().BeTrue();
    }

    [Fact]
    public void Verificar_SenhaErrada_RetornaFalse()
    {
        var hash = _hasher.Hash("senha_correta");
        _hasher.Verificar("senha_errada", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_MesmaSenha_GeraHashesDiferentes()
    {
        var hash1 = _hasher.Hash("senha");
        var hash2 = _hasher.Hash("senha");
        hash1.Should().NotBe(hash2); // BCrypt usa salt aleatório
    }
}
