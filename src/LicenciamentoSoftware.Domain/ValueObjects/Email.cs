using LicenciamentoSoftware.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace LicenciamentoSoftware.Domain.ValueObjects;

/// <summary>
/// Value object para endereço de e-mail.
/// Valida formato básico — domínio com pelo menos um ponto após o @.
/// </summary>
public sealed record Email
{
    private static readonly Regex Formato =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Endereco { get; }

    public Email(string endereco)
    {
        if (string.IsNullOrWhiteSpace(endereco))
            throw new DomainException("E-mail é obrigatório.");

        if (endereco.Length > 300)
            throw new DomainException("E-mail não pode ter mais de 300 caracteres.");

        if (!Formato.IsMatch(endereco))
            throw new DomainException("E-mail inválido.");

        Endereco = endereco.Trim().ToLowerInvariant();
    }

    public override string ToString() => Endereco;
}
