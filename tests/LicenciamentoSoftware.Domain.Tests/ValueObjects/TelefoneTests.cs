using FluentAssertions;
using LicenciamentoSoftware.Domain.Exceptions;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Domain.Tests.ValueObjects;

public class TelefoneTests
{
    [Theory]
    [InlineData("11987654321")]
    [InlineData("1134567890")]
    [InlineData("(11) 98765-4321")]
    [InlineData("(11) 3456-7890")]
    public void Construtor_TelefoneValido_CriaSemExcecao(string numero)
    {
        var tel = new Telefone(numero);
        tel.Numero.Should().MatchRegex(@"^\d{10,11}$");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Construtor_NumeroVazio_LancaDomainException(string? numero)
    {
        var act = () => new Telefone(numero!);
        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("123456789012")]
    public void Construtor_FormatoInvalido_LancaDomainException(string numero)
    {
        var act = () => new Telefone(numero);
        act.Should().Throw<DomainException>().WithMessage("*inválido*");
    }

    [Fact]
    public void Construtor_NumeroComFormatacao_ArmazenaApenasDigitos()
    {
        var tel = new Telefone("(11) 98765-4321");
        tel.Numero.Should().Be("11987654321");
    }
}
