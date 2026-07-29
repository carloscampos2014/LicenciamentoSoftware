using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class LicencaInstalacaoRegistrada
{
    public Guid Id { get; private set; }
    public Guid LicencaId { get; private set; }
    public string IdentificadorMaquina { get; private set; } = string.Empty;
    public DateTime DataRegistro { get; private set; }
    public bool Ativo { get; private set; }

    private LicencaInstalacaoRegistrada() { }

    public static LicencaInstalacaoRegistrada Registrar(Guid licencaId, string identificadorMaquina)
    {
        if (licencaId == Guid.Empty)
            throw new DomainException("LicencaId é obrigatório.");

        if (string.IsNullOrWhiteSpace(identificadorMaquina))
            throw new DomainException("Identificador da máquina é obrigatório.");

        if (identificadorMaquina.Length > 300)
            throw new DomainException("Identificador da máquina não pode ter mais de 300 caracteres.");

        return new LicencaInstalacaoRegistrada
        {
            Id = Guid.NewGuid(),
            LicencaId = licencaId,
            IdentificadorMaquina = identificadorMaquina.Trim(),
            DataRegistro = DateTime.UtcNow,
            Ativo = true
        };
    }

    public void Liberar()
    {
        if (!Ativo)
            throw new DomainException("Instalação já está liberada/inativa.");

        Ativo = false;
    }
}
