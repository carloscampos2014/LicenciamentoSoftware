namespace LicenciamentoSoftware.Domain.Entities;

public class LicencaInstalacao
{
    public Guid Id { get; set; }
    public Guid LicencaId { get; set; }
    public int QuantidadeMaxima { get; set; }

    public Licenca? Licenca { get; set; }
}
