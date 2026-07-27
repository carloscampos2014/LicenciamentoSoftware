namespace LicenciamentoSoftware.Domain.Entities;

public class LicencaSessao
{
    public Guid Id { get; set; }
    public Guid LicencaId { get; set; }
    public string IdentificadorUsuario { get; set; } = string.Empty;
    public DateTime DataLogin { get; set; } = DateTime.UtcNow;
    public DateTime DataUltimaAtividade { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    public Licenca? Licenca { get; set; }
}
