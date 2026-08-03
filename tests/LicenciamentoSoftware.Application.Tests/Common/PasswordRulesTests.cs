using FluentAssertions;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Tests.Common;

public sealed class PasswordRulesTests
{
    // ── IsSenhaForte ─────────────────────────────────────────────────────────

    [Fact]
    public void IsSenhaForte_SenhaVazia_RetornaFalso()
    {
        PasswordRules.IsSenhaForte(string.Empty).Should().BeFalse();
        PasswordRules.IsSenhaForte(null).Should().BeFalse();
        PasswordRules.IsSenhaForte("   ").Should().BeFalse();
    }

    [Fact]
    public void IsSenhaForte_SenhaCurta_RetornaFalso()
    {
        PasswordRules.IsSenhaForte("Ab1!").Should().BeFalse();
        PasswordRules.IsSenhaForte("Ab1!567").Should().BeFalse(); // 7 chars
    }

    [Fact]
    public void IsSenhaForte_SenhaSemMaiuscula_RetornaFalso()
    {
        PasswordRules.IsSenhaForte("abcdef1!").Should().BeFalse();
    }

    [Fact]
    public void IsSenhaForte_SenhaSemNumero_RetornaFalso()
    {
        PasswordRules.IsSenhaForte("Abcdefg!").Should().BeFalse();
    }

    [Fact]
    public void IsSenhaForte_SenhaSemEspecial_RetornaFalso()
    {
        PasswordRules.IsSenhaForte("Abcdef12").Should().BeFalse();
    }

    [Theory]
    [InlineData("Abc@1234")]
    [InlineData("Senha@123")]
    [InlineData("MinhaSenha!9")]
    [InlineData("P@ssw0rd")]
    public void IsSenhaForte_SenhaAtendeTodosOsCriterios_RetornaVerdadeiro(string senha)
    {
        PasswordRules.IsSenhaForte(senha).Should().BeTrue();
    }

    // ── ForcaNivel ───────────────────────────────────────────────────────────

    [Fact]
    public void ForcaNivel_SenhaVazia_RetornaZero()
    {
        PasswordRules.ForcaNivel(null).Should().Be(0);
        PasswordRules.ForcaNivel(string.Empty).Should().Be(0);
    }

    [Fact]
    public void ForcaNivel_ApenasComprimento_RetornaUm()
    {
        PasswordRules.ForcaNivel("abcdefgh").Should().Be(1);
    }

    [Fact]
    public void ForcaNivel_ComprimentoMaiuscula_RetornaDois()
    {
        PasswordRules.ForcaNivel("Abcdefgh").Should().Be(2);
    }

    [Fact]
    public void ForcaNivel_ComprimentoMaiusculaNumero_RetornaTres()
    {
        PasswordRules.ForcaNivel("Abcdef1h").Should().Be(3);
    }

    [Fact]
    public void ForcaNivel_TodosCriterios_RetornaQuatro()
    {
        PasswordRules.ForcaNivel("Abcdef1!").Should().Be(4);
    }

    // ── Mensagem de erro ─────────────────────────────────────────────────────

    [Fact]
    public void MensagemErro_NaoEstaVazia()
    {
        PasswordRules.MensagemErro.Should().NotBeNullOrWhiteSpace();
    }
}
