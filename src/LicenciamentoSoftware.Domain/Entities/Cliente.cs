namespace LicenciamentoSoftware.Domain.Entities;

public class Cliente
{
    public Guid Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public int TipoInscricao { get; set; }
    public string NumeroInscricao { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<ClienteFinal> ClientesFinais { get; set; } = new List<ClienteFinal>();
    public ICollection<Aplicacao> Aplicacoes { get; set; } = new List<Aplicacao>();
    public ICollection<Licenca> Licencas { get; set; } = new List<Licenca>();
}
