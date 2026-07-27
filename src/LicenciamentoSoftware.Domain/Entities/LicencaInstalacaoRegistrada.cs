namespace LicenciamentoSoftware.Domain.Entities;

public class LicencaInstalacaoRegistrada
{
    public Guid Id { get; set; }
    public Guid LicencaId { get; set; }
    public string IdentificadorMaquina { get; set; } = string.Empty;
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    public Licenca? Licenca { get; set; }
}
