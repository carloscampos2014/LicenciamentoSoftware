namespace LicenciamentoSoftware.Domain.Entities;

public class LicencaPeriodo
{
    public Guid Id { get; set; }
    public Guid LicencaId { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool RenovacaoAutomatica { get; set; }

    public Licenca? Licenca { get; set; }
}
