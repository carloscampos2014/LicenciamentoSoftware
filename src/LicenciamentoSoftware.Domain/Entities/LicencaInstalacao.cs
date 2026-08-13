using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class LicencaInstalacao
{
    public Guid Id { get; private set; }
    public Guid LicencaId { get; private set; }
    public int QuantidadeMaxima { get; private set; }

    private LicencaInstalacao() { }

    public static LicencaInstalacao Criar(Guid licencaId, int quantidadeMaxima)
    {
        if (licencaId == Guid.Empty)
            throw new DomainException("LicencaId é obrigatório.");

        if (quantidadeMaxima <= 0)
            throw new DomainException("Quantidade máxima de instalações deve ser maior que zero.");

        return new LicencaInstalacao
        {
            Id = Guid.NewGuid(),
            LicencaId = licencaId,
            QuantidadeMaxima = quantidadeMaxima
        };
    }

    public void Atualizar(int quantidadeMaxima)
    {
        if (quantidadeMaxima <= 0)
            throw new DomainException("Quantidade máxima de instalações deve ser maior que zero.");

        QuantidadeMaxima = quantidadeMaxima;
    }
}
