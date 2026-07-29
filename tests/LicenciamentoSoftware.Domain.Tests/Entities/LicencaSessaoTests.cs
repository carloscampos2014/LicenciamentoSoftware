using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class LicencaSessaoTests
{
    private static readonly Guid LicencaId = Guid.NewGuid();
    private const string IdentificadorValido = "usuario@empresa.com";

    [Fact]
    public void Criar_DadosValidos_RetornaSessaoAtiva()
    {
        var sessao = LicencaSessao.Criar(LicencaId, IdentificadorValido);

        sessao.Id.Should().NotBe(Guid.Empty);
        sessao.LicencaId.Should().Be(LicencaId);
        sessao.IdentificadorUsuario.Should().Be(IdentificadorValido);
        sessao.Ativo.Should().BeTrue();
        sessao.DataLogin.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Criar_IdentificadorVazio_LancaDomainException(string? identificador)
    {
        var act = () => LicencaSessao.Criar(LicencaId, identificador!);
        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Fact]
    public void Criar_LicencaIdVazio_LancaDomainException()
    {
        var act = () => LicencaSessao.Criar(Guid.Empty, IdentificadorValido);
        act.Should().Throw<DomainException>().WithMessage("*LicencaId*");
    }

    [Fact]
    public void Encerrar_SessaoAtiva_TornaInativa()
    {
        var sessao = LicencaSessao.Criar(LicencaId, IdentificadorValido);
        sessao.Encerrar();
        sessao.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Encerrar_SessaoJaEncerrada_LancaDomainException()
    {
        var sessao = LicencaSessao.Criar(LicencaId, IdentificadorValido);
        sessao.Encerrar();

        var act = () => sessao.Encerrar();
        act.Should().Throw<DomainException>().WithMessage("*encerrada*");
    }

    [Fact]
    public void RegistrarAtividade_SessaoAtiva_AtualizaDataUltimaAtividade()
    {
        var sessao = LicencaSessao.Criar(LicencaId, IdentificadorValido);
        var dataAntes = sessao.DataUltimaAtividade;

        // Pequena pausa para garantir diferença de tempo
        Thread.Sleep(10);
        sessao.RegistrarAtividade();

        sessao.DataUltimaAtividade.Should().BeAfter(dataAntes);
    }

    [Fact]
    public void RegistrarAtividade_SessaoInativa_LancaDomainException()
    {
        var sessao = LicencaSessao.Criar(LicencaId, IdentificadorValido);
        sessao.Encerrar();

        var act = () => sessao.RegistrarAtividade();
        act.Should().Throw<DomainException>().WithMessage("*inativa*");
    }
}
