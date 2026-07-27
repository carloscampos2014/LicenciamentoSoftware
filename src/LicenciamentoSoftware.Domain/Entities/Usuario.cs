namespace LicenciamentoSoftware.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public Guid IdCliente { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    public Cliente? Cliente { get; set; }
}
