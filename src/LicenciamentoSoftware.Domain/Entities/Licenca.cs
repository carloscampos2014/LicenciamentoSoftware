namespace LicenciamentoSoftware.Domain.Entities;

// O tipo de licença NÃO fica aqui: é derivado via Aplicativo.IdTipoLicenca,
// evitando redundância/inconsistência entre os dois cadastros.
public class Licenca
{
    public Guid Id { get; set; }
    public Guid IdCliente { get; set; }
    public Guid IdClienteFinal { get; set; }
    public Guid IdAplicativo { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    public Cliente? Cliente { get; set; }
    public ClienteFinal? ClienteFinal { get; set; }
    public Aplicacao? Aplicativo { get; set; }

    public LicencaPeriodo? Periodo { get; set; }
    public LicencaUsuarios? Usuarios { get; set; }
    public LicencaInstalacao? Instalacao { get; set; }
    public ICollection<LicencaSessao> Sessoes { get; set; } = new List<LicencaSessao>();
    public ICollection<LicencaInstalacaoRegistrada> InstalacoesRegistradas { get; set; } = new List<LicencaInstalacaoRegistrada>();
}
