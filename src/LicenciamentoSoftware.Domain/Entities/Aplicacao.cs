using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class Aplicacao
{
    public Guid Id { get; private set; }
    public Guid IdCliente { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public Guid IdTipoLicenca { get; private set; }
    public bool Ativo { get; private set; }

    private Aplicacao() { }

    public static Aplicacao Criar(
        Guid idCliente,
        string titulo,
        Guid idTipoLicenca,
        string? descricao = null)
    {
        if (idCliente == Guid.Empty)
            throw new DomainException("IdCliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("Título é obrigatório.");

        if (titulo.Length > 120)
            throw new DomainException("Título não pode ter mais de 120 caracteres.");

        if (descricao is not null && descricao.Length > 300)
            throw new DomainException("Descrição não pode ter mais de 300 caracteres.");

        if (idTipoLicenca == Guid.Empty)
            throw new DomainException("Tipo de licença é obrigatório.");

        return new Aplicacao
        {
            Id = Guid.NewGuid(),
            IdCliente = idCliente,
            Titulo = titulo.Trim(),
            Descricao = descricao?.Trim(),
            IdTipoLicenca = idTipoLicenca,
            Ativo = true
        };
    }

    public void AtualizarDados(string titulo, string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("Título é obrigatório.");

        if (titulo.Length > 120)
            throw new DomainException("Título não pode ter mais de 120 caracteres.");

        if (descricao is not null && descricao.Length > 300)
            throw new DomainException("Descrição não pode ter mais de 300 caracteres.");

        Titulo = titulo.Trim();
        Descricao = descricao?.Trim();
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("Aplicação já está inativa.");

        Ativo = false;
    }
}
