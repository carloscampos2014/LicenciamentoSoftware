namespace LicenciamentoSoftware.Domain.Entities;

public class LicencaUsuarios
{
    public Guid Id { get; set; }
    public Guid LicencaId { get; set; }
    public int QuantidadeMaxima { get; set; }
    public int MaxSessoesPorUsuario { get; set; } = 5;
    public int TempoLimiteSessaoHoras { get; set; } = 24;

    public Licenca? Licenca { get; set; }
}
