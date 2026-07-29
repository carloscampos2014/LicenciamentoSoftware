using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class LicencaPeriodo
{
    public Guid Id { get; private set; }
    public Guid LicencaId { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }
    public bool RenovacaoAutomatica { get; private set; }

    private LicencaPeriodo() { }

    public static LicencaPeriodo Criar(
        Guid licencaId,
        DateTime dataInicio,
        DateTime dataFim,
        bool renovacaoAutomatica = false)
    {
        if (licencaId == Guid.Empty)
            throw new DomainException("LicencaId é obrigatório.");

        if (dataFim <= dataInicio)
            throw new DomainException("DataFim deve ser posterior a DataInicio.");

        return new LicencaPeriodo
        {
            Id = Guid.NewGuid(),
            LicencaId = licencaId,
            DataInicio = dataInicio,
            DataFim = dataFim,
            RenovacaoAutomatica = renovacaoAutomatica
        };
    }

    public void RenovarPeriodo(DateTime novaDataFim)
    {
        if (novaDataFim <= DataInicio)
            throw new DomainException("Nova DataFim deve ser posterior a DataInicio.");

        DataFim = novaDataFim;
    }
}
