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
}
