using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class LicencaInstalacaoTests
{
    private static readonly Guid LicencaId = Guid.NewGuid();

    [Fact]
    public void Criar_DadosValidos_RetornaDetalhe()
    {
        var detalhe = LicencaInstalacao.Criar(LicencaId, quantidadeMaxima: 5);

        detalhe.Id.Should().NotBe(Guid.Empty);
        detalhe.QuantidadeMaxima.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Criar_QuantidadeMaximaZeroOuNegativa_LancaDomainException(int quantidade)
    {
        var act = () => LicencaInstalacao.Criar(LicencaId, quantidade);
        act.Should().Throw<DomainException>().WithMessage("*maior que zero*");
    }

    [Fact]
    public void Criar_LicencaIdVazio_LancaDomainException()
    {
        var act = () => LicencaInstalacao.Criar(Guid.Empty, 5);
        act.Should().Throw<DomainException>().WithMessage("*LicencaId*");
    }
}
