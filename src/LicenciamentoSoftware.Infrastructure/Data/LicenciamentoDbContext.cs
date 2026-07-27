using LicenciamentoSoftware.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Infrastructure.Data;

public class LicenciamentoDbContext : DbContext
{
    public LicenciamentoDbContext(DbContextOptions<LicenciamentoDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ClienteFinal> ClientesFinais => Set<ClienteFinal>();
    public DbSet<Aplicacao> Aplicacoes => Set<Aplicacao>();
    public DbSet<TipoLicenca> TiposLicenca => Set<TipoLicenca>();
    public DbSet<Licenca> Licencas => Set<Licenca>();
    public DbSet<LicencaPeriodo> LicencasPeriodo => Set<LicencaPeriodo>();
    public DbSet<LicencaUsuarios> LicencasUsuarios => Set<LicencaUsuarios>();
    public DbSet<LicencaSessao> LicencasSessao => Set<LicencaSessao>();
    public DbSet<LicencaInstalacao> LicencasInstalacao => Set<LicencaInstalacao>();
    public DbSet<LicencaInstalacaoRegistrada> LicencasInstalacaoRegistrada => Set<LicencaInstalacaoRegistrada>();
    public DbSet<LogOperacao> LogsOperacao => Set<LogOperacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Cliente");
            e.HasKey(x => x.Id);
            e.Property(x => x.RazaoSocial).HasMaxLength(200).IsRequired();
            e.Property(x => x.NumeroInscricao).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(300).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(15);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Cliente).WithMany(c => c.Usuarios).HasForeignKey(x => x.IdCliente);
            e.HasIndex(x => x.IdCliente);
        });

        modelBuilder.Entity<ClienteFinal>(e =>
        {
            e.ToTable("ClienteFinal");
            e.HasKey(x => x.Id);
            e.Property(x => x.RazaoSocial).HasMaxLength(200).IsRequired();
            e.Property(x => x.NumeroInscricao).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(300).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(15);
            e.HasOne(x => x.Cliente).WithMany(c => c.ClientesFinais).HasForeignKey(x => x.IdCliente);
            e.HasIndex(x => x.IdCliente);
        });

        modelBuilder.Entity<TipoLicenca>(e =>
        {
            e.ToTable("TipoLicenca");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();

            // Seed fixo/global.
            e.HasData(
                new TipoLicenca { Id = TipoLicenca.Permanente, Descricao = "Permanente" },
                new TipoLicenca { Id = TipoLicenca.PorPeriodo, Descricao = "Por Período" },
                new TipoLicenca { Id = TipoLicenca.PorUsuarios, Descricao = "Por Usuários" },
                new TipoLicenca { Id = TipoLicenca.PorInstalacao, Descricao = "Por Instalação" }
            );
        });

        modelBuilder.Entity<Aplicacao>(e =>
        {
            e.ToTable("Aplicacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(120).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.HasOne(x => x.Cliente).WithMany(c => c.Aplicacoes).HasForeignKey(x => x.IdCliente);
            e.HasOne(x => x.TipoLicenca).WithMany().HasForeignKey(x => x.IdTipoLicenca);
            e.HasIndex(x => x.IdCliente);
            e.HasIndex(x => x.IdTipoLicenca);
        });

        modelBuilder.Entity<Licenca>(e =>
        {
            e.ToTable("Licenca");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Cliente).WithMany(c => c.Licencas).HasForeignKey(x => x.IdCliente);
            e.HasOne(x => x.ClienteFinal).WithMany(c => c.Licencas).HasForeignKey(x => x.IdClienteFinal);
            e.HasOne(x => x.Aplicativo).WithMany(a => a.Licencas).HasForeignKey(x => x.IdAplicativo);

            // Só pode existir uma licença ATIVA por combinação Cliente+ClienteFinal+Aplicativo.
            // É essa combinação que a API de validação usa para localizar a licença.
            e.HasIndex(x => new { x.IdCliente, x.IdClienteFinal, x.IdAplicativo })
                .IsUnique()
                .HasFilter("\"Ativo\" = true")
                .HasDatabaseName("uq_licenca_combinacao_ativa");
        });

        modelBuilder.Entity<LicencaPeriodo>(e =>
        {
            e.ToTable("LicencaPeriodo");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Licenca).WithOne(l => l.Periodo).HasForeignKey<LicencaPeriodo>(x => x.LicencaId);
            e.HasIndex(x => x.LicencaId).IsUnique();
        });

        modelBuilder.Entity<LicencaUsuarios>(e =>
        {
            e.ToTable("LicencaUsuarios");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Licenca).WithOne(l => l.Usuarios).HasForeignKey<LicencaUsuarios>(x => x.LicencaId);
            e.HasIndex(x => x.LicencaId).IsUnique();
        });

        modelBuilder.Entity<LicencaSessao>(e =>
        {
            e.ToTable("LicencaSessao");
            e.HasKey(x => x.Id);
            e.Property(x => x.IdentificadorUsuario).HasMaxLength(300).IsRequired();
            e.HasOne(x => x.Licenca).WithMany(l => l.Sessoes).HasForeignKey(x => x.LicencaId);
            e.HasIndex(x => new { x.LicencaId, x.Ativo });
            e.HasIndex(x => new { x.LicencaId, x.IdentificadorUsuario, x.Ativo });
        });

        modelBuilder.Entity<LicencaInstalacao>(e =>
        {
            e.ToTable("LicencaInstalacao");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Licenca).WithOne(l => l.Instalacao).HasForeignKey<LicencaInstalacao>(x => x.LicencaId);
            e.HasIndex(x => x.LicencaId).IsUnique();
        });

        modelBuilder.Entity<LicencaInstalacaoRegistrada>(e =>
        {
            e.ToTable("LicencaInstalacaoRegistrada");
            e.HasKey(x => x.Id);
            e.Property(x => x.IdentificadorMaquina).HasMaxLength(300).IsRequired();
            e.HasOne(x => x.Licenca).WithMany(l => l.InstalacoesRegistradas).HasForeignKey(x => x.LicencaId);
            e.HasIndex(x => new { x.LicencaId, x.IdentificadorMaquina })
                .IsUnique()
                .HasFilter("\"Ativo\" = true")
                .HasDatabaseName("uq_licencainstalacao_maquina_ativa");
        });

        modelBuilder.Entity<LogOperacao>(e =>
        {
            e.ToTable("LogOperacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Entidade).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            e.HasIndex(x => new { x.Entidade, x.IdRegistro });
        });
    }
}
