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

    // CNPJ numérico válido de teste
    private const string CnpjValido = "11.222.333/0001-81";
    private const string CnpjValidoDigitos = "11222333000181";

    // CNPJ alfanumérico válido — IN RFB 2.229/2024
    // Raiz alfanumérica "B3LH8F120001", verificadores calculados: 80
    private const string CnpjAlfanumericoValido        = "B3.LH8.F12/0001-80";
    private const string CnpjAlfanumericoValidoNumero  = "B3LH8F12000180";

    // -------------------------------------------------------------------------
    // CPF
    // -------------------------------------------------------------------------

    [Fact]
    public void Construtor_CpfValido_CriaSemExcecao()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaFisica, CpfValido);
        inscricao.Numero.Should().Be(CpfValidoDigitos);
        inscricao.Tipo.Should().Be(TipoInscricao.PessoaFisica);
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

    [Fact]
    public void Construtor_CpfComFormatacao_ArmazenaApenasDigitos()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaFisica, CpfValido);
        inscricao.Numero.Should().MatchRegex(@"^\d+$");
    }

    // -------------------------------------------------------------------------
    // CNPJ numérico (compatibilidade)
    // -------------------------------------------------------------------------

    [Fact]
    public void Construtor_CnpjNumericoValido_CriaSemExcecao()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaJuridica, CnpjValido);
        inscricao.Numero.Should().Be(CnpjValidoDigitos);
        inscricao.Tipo.Should().Be(TipoInscricao.PessoaJuridica);
    }

    [Theory]
    [InlineData("11.111.111/1111-11")] // dígitos iguais
    [InlineData("00.000.000/0000-00")]
    [InlineData("12.345.678/0001-00")] // dígito verificador errado
    public void Construtor_CnpjNumericoInvalido_LancaDomainException(string cnpj)
    {
        var act = () => new Inscricao(TipoInscricao.PessoaJuridica, cnpj);
        act.Should().Throw<DomainException>().WithMessage("*CNPJ*");
    }

    [Fact]
    public void Construtor_CnpjNumericoComFormatacao_ArmazenaApenasDigitos()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaJuridica, CnpjValido);
        inscricao.Numero.Should().MatchRegex(@"^\d+$");
    }

    // -------------------------------------------------------------------------
    // CNPJ alfanumérico — IN RFB 2.229/2024
    // -------------------------------------------------------------------------

    [Fact]
    public void Construtor_CnpjAlfanumericoValido_CriaSemExcecao()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaJuridica, CnpjAlfanumericoValido);
        inscricao.Numero.Should().Be(CnpjAlfanumericoValidoNumero);
        inscricao.Tipo.Should().Be(TipoInscricao.PessoaJuridica);
    }

    [Fact]
    public void Construtor_CnpjAlfanumericoMinusculo_NormalizaParaMaiusculo()
    {
        // "b3lh8f12/0001-80" deve ser normalizado para "B3LH8F12000180"
        var inscricao = new Inscricao(TipoInscricao.PessoaJuridica,
            CnpjAlfanumericoValido.ToLowerInvariant());
        inscricao.Numero.Should().Be(CnpjAlfanumericoValidoNumero);
    }

    [Fact]
    public void Construtor_CnpjAlfanumericoComFormatacao_RemoveFormatacaoPreservaLetras()
    {
        var inscricao = new Inscricao(TipoInscricao.PessoaJuridica, CnpjAlfanumericoValido);
        // Deve conter letras mas não pontuação
        inscricao.Numero.Should().NotContain(".");
        inscricao.Numero.Should().NotContain("/");
        inscricao.Numero.Should().NotContain("-");
        inscricao.Numero.Should().HaveLength(14);
    }

    [Theory]
    [InlineData("AA.AAA.AAA/AAAA-AA")] // todos iguais
    [InlineData("ZZ.ZZZ.ZZ1/0001-00")] // dígito verificador errado
    public void Construtor_CnpjAlfanumericoInvalido_LancaDomainException(string cnpj)
    {
        var act = () => new Inscricao(TipoInscricao.PessoaJuridica, cnpj);
        act.Should().Throw<DomainException>().WithMessage("*CNPJ*");
    }

    [Fact]
    public void Construtor_CnpjComCaracteresEspeciaisInvalidos_LancaDomainException()
    {
        // Caracteres que não são dígitos nem letras A-Z devem ser rejeitados
        var act = () => new Inscricao(TipoInscricao.PessoaJuridica, "12@345678000195");
        act.Should().Throw<DomainException>().WithMessage("*CNPJ*");
    }
}
