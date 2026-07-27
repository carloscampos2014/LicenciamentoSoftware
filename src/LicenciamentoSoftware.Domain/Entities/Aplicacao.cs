namespace LicenciamentoSoftware.Domain.Entities;

public class Aplicacao
{
    public Guid Id { get; set; }
    public Guid IdCliente { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public Guid IdTipoLicenca { get; set; }
    public bool Ativo { get; set; } = true;

    public Cliente? Cliente { get; set; }
    public TipoLicenca? TipoLicenca { get; set; }
    public ICollection<Licenca> Licencas { get; set; } = new List<Licenca>();
}
