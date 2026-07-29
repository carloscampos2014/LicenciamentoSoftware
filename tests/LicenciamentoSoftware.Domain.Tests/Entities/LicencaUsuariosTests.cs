using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class LicencaUsuariosTests
{
    private static readonly Guid LicencaId = Guid.NewGuid();

    [Fact]
    public void Criar_DadosValidos_RetornaDetalhe()
    {
        var detalhe = LicencaUsuarios.Criar(LicencaId, quantidadeMaxima: 10);

        detalhe.Id.Should().NotBe(Guid.Empty);
        detalhe.QuantidadeMaxima.Should().Be(10);
        detalhe.MaxSessoesPorUsuario.Should().Be(5);
        detalhe.TempoLimiteSessaoHoras.Should().Be(24);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Criar_QuantidadeMaximaZeroOuNegativa_LancaDomainException(int quantidade)
    {
        var act = () => LicencaUsuarios.Criar(LicencaId, quantidade);
        act.Should().Throw<DomainException>().WithMessage("*maior que zero*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_MaxSessoesPorUsuarioZeroOuNegativo_LancaDomainException(int maxSessoes)
    {
        var act = () => LicencaUsuarios.Criar(LicencaId, 10, maxSessoesPorUsuario: maxSessoes);
        act.Should().Throw<DomainException>().WithMessage("*sessões*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_TempoLimiteZeroOuNegativo_LancaDomainException(int tempoLimite)
    {
        var act = () => LicencaUsuarios.Criar(LicencaId, 10, tempoLimiteSessaoHoras: tempoLimite);
        act.Should().Throw<DomainException>().WithMessage("*limite*");
    }

    [Fact]
    public void Criar_LicencaIdVazio_LancaDomainException()
    {
        var act = () => LicencaUsuarios.Criar(Guid.Empty, 10);
        act.Should().Throw<DomainException>().WithMessage("*LicencaId*");
    }
}
