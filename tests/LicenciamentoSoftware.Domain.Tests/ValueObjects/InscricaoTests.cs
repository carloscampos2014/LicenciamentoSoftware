using FluentAssertions;
using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.Exceptions;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Domain.Tests.ValueObjects;

public class InscricaoTests
{
    // CPF válido de teste (algoritmo)
    private const string CpfValido = "529.982.247-25";
    private const string CpfValidoDigitos = "52998224725";

    // CNPJ válido de teste (algoritmo)
    private const string CnpjValido = "11.222.333/0001-81";
    private const string CnpjValidoDigitos = "11222333000181";

    [Fact]
    public void Construtor_CpfValido_CriaSemExcecao()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaFisica, CpfValido);
        inscricao.Numero.Should().Be(CpfValidoDigitos);
        inscricao.Tipo.Should().Be(TipoInscricao.PessoaFisica);
    }

    [Fact]
    public void Construtor_CnpjValido_CriaSemExcecao()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaJuridica, CnpjValido);
        inscricao.Numero.Should().Be(CnpjValidoDigitos);
        inscricao.Tipo.Should().Be(TipoInscricao.PessoaJuridica);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Construtor_NumeroVazio_LancaDomainException(string? numero)
    {
        var act = () => new Inscricao(TipoInscricao.PessoaFisica, numero!);
        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Theory]
    [InlineData("111.111.111-11")] // dígitos iguais
    [InlineData("000.000.000-00")]
    [InlineData("123.456.789-00")] // dígito verificador errado
    public void Construtor_CpfInvalido_LancaDomainException(string cpf)
    {
        var act = () => new Inscricao(TipoInscricao.PessoaFisica, cpf);
        act.Should().Throw<DomainException>().WithMessage("*CPF*");
    }

    [Theory]
    [InlineData("11.111.111/1111-11")] // dígitos iguais
    [InlineData("00.000.000/0000-00")]
    [InlineData("12.345.678/0001-00")] // dígito verificador errado
    public void Construtor_CnpjInvalido_LancaDomainException(string cnpj)
    {
        var act = () => new Inscricao(TipoInscricao.PessoaJuridica, cnpj);
        act.Should().Throw<DomainException>().WithMessage("*CNPJ*");
    }

    [Fact]
    public void Construtor_NumeroComFormatacao_ArmazenaApenasDigitos()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaFisica, CpfValido);
        inscricao.Numero.Should().MatchRegex(@"^\d+$");
    }
}
