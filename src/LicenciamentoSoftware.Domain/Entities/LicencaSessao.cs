using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class LicencaSessao
{
    public Guid Id { get; private set; }
    public Guid LicencaId { get; private set; }
    public string IdentificadorUsuario { get; private set; } = string.Empty;
    public DateTime DataLogin { get; private set; }
    public DateTime DataUltimaAtividade { get; private set; }
    public bool Ativo { get; private set; }

    private LicencaSessao() { }

    public static LicencaSessao Criar(Guid licencaId, string identificadorUsuario)
    {
        if (licencaId == Guid.Empty)
            throw new DomainException("LicencaId é obrigatório.");

        if (string.IsNullOrWhiteSpace(identificadorUsuario))
            throw new DomainException("Identificador do usuário é obrigatório.");

        if (identificadorUsuario.Length > 300)
            throw new DomainException("Identificador do usuário não pode ter mais de 300 caracteres.");

        var agora = DateTime.UtcNow;

        return new LicencaSessao
        {
            Id = Guid.NewGuid(),
            LicencaId = licencaId,
            IdentificadorUsuario = identificadorUsuario.Trim(),
            DataLogin = agora,
            DataUltimaAtividade = agora,
            Ativo = true
        };
    }

    public void RegistrarAtividade()
    {
        if (!Ativo)
            throw new DomainException("Não é possível registrar atividade em sessão inativa.");

        DataUltimaAtividade = DateTime.UtcNow;
    }

    public void Encerrar()
    {
        if (!Ativo)
            throw new DomainException("Sessão já está encerrada.");

        Ativo = false;
    }
}
