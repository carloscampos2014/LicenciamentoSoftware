using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

/// <summary>
/// Token HMAC-SHA256 vinculado a uma licença.
/// O valor em texto do segredo é gerado fora do domínio e exibido
/// uma única vez na emissão — aqui armazenamos apenas o hash.
/// </summary>
public sealed class LicencaToken
{
    public Guid Id { get; private set; }
    public Guid IdLicenca { get; private set; }
    public string SegredoHash { get; private set; } = string.Empty;
    public int ExpiracaoMinutos { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public bool Ativo { get; private set; }

    private LicencaToken() { }

    /// <summary>
    /// Cria um novo token HMAC para a licença informada.
    /// </summary>
    /// <param name="idLicenca">ID da licença dona do token.</param>
    /// <param name="segredoHash">Hash BCrypt do segredo gerado pelo serviço.</param>
    /// <param name="expiracaoMinutos">Janela de validade do token em minutos.</param>
    public static LicencaToken Criar(Guid idLicenca, string segredoHash, int expiracaoMinutos)
    {
        if (idLicenca == Guid.Empty)
            throw new DomainException("IdLicenca é obrigatório.");

        if (string.IsNullOrWhiteSpace(segredoHash))
            throw new DomainException("SegredoHash é obrigatório.");

        if (expiracaoMinutos <= 0)
            throw new DomainException("ExpiracaoMinutos deve ser maior que zero.");

        return new LicencaToken
        {
            Id = Guid.NewGuid(),
            IdLicenca = idLicenca,
            SegredoHash = segredoHash,
            ExpiracaoMinutos = expiracaoMinutos,
            CriadoEm = DateTime.UtcNow,
            Ativo = true,
        };
    }

    /// <summary>Revoga o token, impedindo sua utilização futura.</summary>
    public void Revogar()
    {
        if (!Ativo)
            throw new DomainException("Token já está revogado.");

        Ativo = false;
    }

    /// <summary>Atualiza o segredo e o tempo de expiração (renovação).</summary>
    public void Renovar(string novoSegredoHash, int expiracaoMinutos)
    {
        if (string.IsNullOrWhiteSpace(novoSegredoHash))
            throw new DomainException("NovoSegredoHash é obrigatório.");

        if (expiracaoMinutos <= 0)
            throw new DomainException("ExpiracaoMinutos deve ser maior que zero.");

        SegredoHash = novoSegredoHash;
        ExpiracaoMinutos = expiracaoMinutos;
        CriadoEm = DateTime.UtcNow;
        Ativo = true;
    }
}
