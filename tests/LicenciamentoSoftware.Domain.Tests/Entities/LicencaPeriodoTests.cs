using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class LicencaPeriodoTests
{
    private static readonly Guid LicencaId = Guid.NewGuid();
    private static readonly DateTime DataInicio = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DataFimValida = new(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_DatasValidas_RetornaDetalhe()
    {
        var detalhe = LicencaPeriodo.Criar(LicencaId, DataInicio, DataFimValida);

        detalhe.Id.Should().NotBe(Guid.Empty);
        detalhe.DataInicio.Should().Be(DataInicio);
        detalhe.DataFim.Should().Be(DataFimValida);
        detalhe.RenovacaoAutomatica.Should().BeFalse();
    }

    [Fact]
    public void Criar_DataFimIgualDataInicio_LancaDomainException()
    {
        var act = () => LicencaPeriodo.Criar(LicencaId, DataInicio, DataInicio);
        act.Should().Throw<DomainException>().WithMessage("*posterior*");
    }

    [Fact]
    public void Criar_DataFimAnteriorDataInicio_LancaDomainException()
    {
        var dataFimAnterior = DataInicio.AddDays(-1);
        var act = () => LicencaPeriodo.Criar(LicencaId, DataInicio, dataFimAnterior);
        act.Should().Throw<DomainException>().WithMessage("*posterior*");
    }

    [Fact]
    public void Criar_LicencaIdVazio_LancaDomainException()
    {
        var act = () => LicencaPeriodo.Criar(Guid.Empty, DataInicio, DataFimValida);
        act.Should().Throw<DomainException>().WithMessage("*LicencaId*");
    }

    [Fact]
    public void Criar_ComRenovacaoAutomatica_DefineComoTrue()
    {
        var detalhe = LicencaPeriodo.Criar(LicencaId, DataInicio, DataFimValida, renovacaoAutomatica: true);
        detalhe.RenovacaoAutomatica.Should().BeTrue();
    }

    [Fact]
    public void RenovarPeriodo_NovaDataFimValida_AtualizaDataFim()
    {
        var detalhe = LicencaPeriodo.Criar(LicencaId, DataInicio, DataFimValida);
        var novaDataFim = DataFimValida.AddYears(1);

        detalhe.RenovarPeriodo(novaDataFim);

        detalhe.DataFim.Should().Be(novaDataFim);
    }

    [Fact]
    public void RenovarPeriodo_NovaDataFimAnteriorAoInicio_LancaDomainException()
    {
        var detalhe = LicencaPeriodo.Criar(LicencaId, DataInicio, DataFimValida);
        var act = () => detalhe.RenovarPeriodo(DataInicio.AddDays(-1));
        act.Should().Throw<DomainException>().WithMessage("*posterior*");
    }
}
