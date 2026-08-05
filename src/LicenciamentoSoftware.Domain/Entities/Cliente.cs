using LicenciamentoSoftware.Domain.Exceptions;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class Cliente
{
    public Guid Id { get; private set; }
    public string RazaoSocial { get; private set; } = string.Empty;
    public Inscricao Inscricao { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Telefone? Telefone { get; private set; }
    public bool Ativo { get; private set; }

    /// <summary>Data/hora UTC em que a conta foi encerrada. Nulo enquanto ativa.</summary>
    public DateTime? EncerradoEm { get; private set; }

    /// <summary>
    /// Data/hora UTC agendada para exclusão física dos dados.
    /// Nulo enquanto ativa. Preenchida no encerramento.
    /// </summary>
    public DateTime? ExclusaoProgramadaEm { get; private set; }

    private Cliente() { }

    public static Cliente Criar(
        string razaoSocial,
        Inscricao inscricao,
        Email email,
        Telefone? telefone = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new DomainException("Razão social é obrigatória.");

        if (razaoSocial.Length > 200)
            throw new DomainException("Razão social não pode ter mais de 200 caracteres.");

        return new Cliente
        {
            Id = Guid.NewGuid(),
            RazaoSocial = razaoSocial.Trim(),
            Inscricao = inscricao,
            Email = email,
            Telefone = telefone,
            Ativo = true
        };
    }

    public void AtualizarDados(string razaoSocial, Email email, Telefone? telefone = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new DomainException("Razão social é obrigatória.");

        if (razaoSocial.Length > 200)
            throw new DomainException("Razão social não pode ter mais de 200 caracteres.");

        RazaoSocial = razaoSocial.Trim();
        Email = email;
        Telefone = telefone;
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("Cliente já está inativo.");

        Ativo = false;
    }

    /// <summary>
    /// Encerra a conta da empresa: desativa o cliente e agenda exclusão física.
    /// </summary>
    /// <param name="exclusaoImediata">
    /// Se verdadeiro, <see cref="ExclusaoProgramadaEm"/> = <paramref name="agora"/>;
    /// o job diário excluirá os dados na próxima execução.
    /// Se falso, a exclusão é agendada para <paramref name="agora"/> + 90 dias.
    /// </param>
    /// <param name="agora">Data/hora UTC atual (injetada para testabilidade).</param>
    public void EncerrarConta(bool exclusaoImediata, DateTime agora)
    {
        if (!Ativo)
            throw new DomainException("Cliente já está inativo.");

        Ativo                = false;
        EncerradoEm          = agora;
        ExclusaoProgramadaEm = exclusaoImediata
            ? agora
            : agora.AddDays(90);
    }
}
