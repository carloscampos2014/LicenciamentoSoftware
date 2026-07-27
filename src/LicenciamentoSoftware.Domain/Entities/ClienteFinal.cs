namespace LicenciamentoSoftware.Domain.Entities;

public class ClienteFinal
{
    public Guid Id { get; set; }
    public Guid IdCliente { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public int TipoInscricao { get; set; }
    public string NumeroInscricao { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;

    public Cliente? Cliente { get; set; }
    public ICollection<Licenca> Licencas { get; set; } = new List<Licenca>();
}
