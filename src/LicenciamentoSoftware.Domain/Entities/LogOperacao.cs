namespace LicenciamentoSoftware.Domain.Entities;

public class LogOperacao
{
    public Guid Id { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public Guid IdRegistro { get; set; }
    public char Operacao { get; set; } // 'I' | 'U' | 'D'
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public Guid? IdUsuario { get; set; }
    public string? CamposAlterados { get; set; } // JSON

    public Usuario? Usuario { get; set; }
}
