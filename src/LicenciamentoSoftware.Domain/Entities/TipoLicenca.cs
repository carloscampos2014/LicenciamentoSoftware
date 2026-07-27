namespace LicenciamentoSoftware.Domain.Entities;

// Tabela fixa/global (seed). Ids conhecidos - ver LicenciamentoDbContext.
public class TipoLicenca
{
    public static readonly Guid Permanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PorPeriodo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid PorUsuarios = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PorInstalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
