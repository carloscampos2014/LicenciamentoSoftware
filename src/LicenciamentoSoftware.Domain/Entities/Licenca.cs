using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class Licenca
{
    public Guid Id { get; private set; }
    public Guid IdCliente { get; private set; }
    public Guid IdClienteFinal { get; private set; }
    public Guid IdAplicativo { get; private set; }
    public DateTime DataCadastro { get; private set; }
    public bool Ativo { get; private set; }

    private Licenca() { }

    public static Licenca Criar(Guid idCliente, Guid idClienteFinal, Guid idAplicativo)
    {
        if (idCliente == Guid.Empty)
            throw new DomainException("IdCliente é obrigatório.");

        if (idClienteFinal == Guid.Empty)
            throw new DomainException("IdClienteFinal é obrigatório.");

        if (idAplicativo == Guid.Empty)
            throw new DomainException("IdAplicativo é obrigatório.");

        return new Licenca
        {
            Id = Guid.NewGuid(),
            IdCliente = idCliente,
            IdClienteFinal = idClienteFinal,
            IdAplicativo = idAplicativo,
            DataCadastro = DateTime.UtcNow,
            Ativo = true
        };
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("Licença já está inativa.");

        Ativo = false;
    }
}
