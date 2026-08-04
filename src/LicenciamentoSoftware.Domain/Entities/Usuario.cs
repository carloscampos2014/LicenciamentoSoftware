using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class Usuario
{
    public Guid Id { get; private set; }
    public Guid IdCliente { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public string? TotpSecretHash { get; private set; }
    public bool Ativo { get; private set; }

    // LGPD — Art. 7 e 8: registro de consentimento
    public bool LgpdAceito { get; private set; }
    public DateTime? LgpdAceitoEm { get; private set; }
    public string? LgpdIpOrigem { get; private set; }

    private Usuario() { }

    public static Usuario Criar(Guid idCliente, string nome, string senhaHash, string email = "")
    {
        if (idCliente == Guid.Empty)
            throw new DomainException("IdCliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome é obrigatório.");

        if (nome.Length > 200)
            throw new DomainException("Nome não pode ter mais de 200 caracteres.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("Hash de senha é obrigatório.");

        return new Usuario
        {
            Id = Guid.NewGuid(),
            IdCliente = idCliente,
            Nome = nome.Trim(),
            Email = email.Trim(),
            SenhaHash = senhaHash,
            TotpSecretHash = null,
            Ativo = true
        };
    }

    public void AtualizarDados(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome é obrigatório.");

        if (nome.Length > 200)
            throw new DomainException("Nome não pode ter mais de 200 caracteres.");

        Nome = nome.Trim();
    }

    public void DefinirSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("Hash de senha é obrigatório.");

        SenhaHash = senhaHash;
    }

    public void DefinirTotpSecret(string totpSecretHash)
    {
        if (string.IsNullOrWhiteSpace(totpSecretHash))
            throw new DomainException("Hash do segredo TOTP é obrigatório.");

        TotpSecretHash = totpSecretHash;
    }

    public void RemoverTotpSecret() => TotpSecretHash = null;

    public void RegistrarConsentimento(DateTime aceiteEm, string ipOrigem)
    {
        if (string.IsNullOrWhiteSpace(ipOrigem))
            throw new DomainException("IP de origem é obrigatório para o registro de consentimento.");

        LgpdAceito = true;
        LgpdAceitoEm = aceiteEm.ToUniversalTime();
        LgpdIpOrigem = ipOrigem.Trim();
    }

    /// <summary>
    /// LGPD Art. 18 — Anonimiza dados pessoais do titular.
    /// Substitui nome e email pelos dados da empresa (para AdministradorCliente)
    /// ou por valores genéricos (para demais papéis), e apaga credenciais.
    /// </summary>
    public void AnonimizarDados(string nomeSubstituto, string emailSubstituto)
    {
        Nome = nomeSubstituto;
        Email = emailSubstituto;
        SenhaHash = string.Empty;
        TotpSecretHash = null;
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("Usuário já está inativo.");

        Ativo = false;
    }
}
