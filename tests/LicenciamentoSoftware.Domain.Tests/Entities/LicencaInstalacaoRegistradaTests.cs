using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class LicencaInstalacaoRegistradaTests
{
    private static readonly Guid LicencaId = Guid.NewGuid();

    [Fact]
    public void Registrar_DadosValidos_RetornaMaquinaAtiva()
    {
        var maquina = LicencaInstalacaoRegistrada.Registrar(LicencaId, "MAQUINA-001");

        maquina.Id.Should().NotBe(Guid.Empty);
        maquina.IdentificadorMaquina.Should().Be("MAQUINA-001");
        maquina.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Registrar_IdentificadorVazio_LancaDomainException(string? identificador)
    {
        var act = () => LicencaInstalacaoRegistrada.Registrar(LicencaId, identificador!);
        act.Should().Throw<DomainException>().WithMessage("*obrigatório*");
    }

    [Fact]
    public void Liberar_MaquinaAtiva_TornaInativa()
    {
        var maquina = LicencaInstalacaoRegistrada.Registrar(LicencaId, "MAQUINA-001");
        maquina.Liberar();
        maquina.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Liberar_MaquinaJaLiberada_LancaDomainException()
    {
        var maquina = LicencaInstalacaoRegistrada.Registrar(LicencaId, "MAQUINA-001");
        maquina.Liberar();

        var act = () => maquina.Liberar();
        act.Should().Throw<DomainException>().WithMessage("*liberada*");
    }
}
