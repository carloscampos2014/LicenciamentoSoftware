using FluentAssertions;
using LicenciamentoSoftware.Domain.Exceptions;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("usuario@empresa.com")]
    [InlineData("USUARIO@EMPRESA.COM.BR")]
    [InlineData("nome.sobrenome@dominio.org")]
    public void Construtor_EmailValido_CriaSemExcecao(string endereco)
    {
        var email = new Email(endereco);
        email.Endereco.Should().Be(endereco.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Construtor_EnderecoVazio_LancaDomainException(string? endereco)
    {
        var act = () => new Email(endereco!);
        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Theory]
    [InlineData("semArroba")]
    [InlineData("@semdominio.com")]
    [InlineData("sem@ponto")]
    [InlineData("dois@@arrobas.com")]
    public void Construtor_FormatoInvalido_LancaDomainException(string endereco)
    {
        var act = () => new Email(endereco);
        act.Should().Throw<DomainException>().WithMessage("*inválido*");
    }

    [Fact]
    public void Construtor_EnderecoMuitoLongo_LancaDomainException()
    {
        // 295 'a' + "@b.com" = 301 chars — acima do limite de 300
        var endereco = new string('a', 295) + "@b.com";
        var act = () => new Email(endereco);
        act.Should().Throw<DomainException>().WithMessage("*300*");
    }

    [Fact]
    public void Construtor_EmailNormalizado_EmLetrasMinusculas()
    {
        var email = new Email("TESTE@DOMINIO.COM");
        email.Endereco.Should().Be("teste@dominio.com");
    }
}
