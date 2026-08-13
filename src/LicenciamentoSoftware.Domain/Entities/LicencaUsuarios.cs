using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class LicencaUsuarios
{
    public Guid Id { get; private set; }
    public Guid LicencaId { get; private set; }
    public int QuantidadeMaxima { get; private set; }
    public int MaxSessoesPorUsuario { get; private set; }
    public int TempoLimiteSessaoHoras { get; private set; }

    private LicencaUsuarios() { }

    public static LicencaUsuarios Criar(
        Guid licencaId,
        int quantidadeMaxima,
        int maxSessoesPorUsuario = 5,
        int tempoLimiteSessaoHoras = 24)
    {
        if (licencaId == Guid.Empty)
            throw new DomainException("LicencaId é obrigatório.");

        if (quantidadeMaxima <= 0)
            throw new DomainException("Quantidade máxima de usuários deve ser maior que zero.");

        if (maxSessoesPorUsuario <= 0)
            throw new DomainException("Máximo de sessões por usuário deve ser maior que zero.");

        if (tempoLimiteSessaoHoras <= 0)
            throw new DomainException("Tempo limite de sessão deve ser maior que zero.");

        return new LicencaUsuarios
        {
            Id = Guid.NewGuid(),
            LicencaId = licencaId,
            QuantidadeMaxima = quantidadeMaxima,
            MaxSessoesPorUsuario = maxSessoesPorUsuario,
            TempoLimiteSessaoHoras = tempoLimiteSessaoHoras
        };
    }

    public void Atualizar(int quantidadeMaxima, int maxSessoesPorUsuario)
    {
        if (quantidadeMaxima <= 0)
            throw new DomainException("Quantidade máxima de usuários deve ser maior que zero.");

        if (maxSessoesPorUsuario <= 0)
            throw new DomainException("Máximo de sessões por usuário deve ser maior que zero.");

        QuantidadeMaxima = quantidadeMaxima;
        MaxSessoesPorUsuario = maxSessoesPorUsuario;
    }
}
