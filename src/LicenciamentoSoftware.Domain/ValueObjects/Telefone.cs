using LicenciamentoSoftware.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace LicenciamentoSoftware.Domain.ValueObjects;

/// <summary>
/// Value object para número de telefone brasileiro.
/// Aceita formatos: (XX) XXXXX-XXXX, (XX) XXXX-XXXX ou apenas dígitos (10-11).
/// Armazena apenas dígitos.
/// </summary>
public sealed record Telefone
{
    private static readonly Regex ApenasDigitos = new(@"^\d{10,11}$", RegexOptions.Compiled);

    public string Numero { get; }

    public Telefone(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("Telefone é obrigatório.");

        var digits = Regex.Replace(numero, @"\D", "");

        if (!ApenasDigitos.IsMatch(digits))
            throw new DomainException("Telefone inválido. Informe DDD + número (10 ou 11 dígitos).");

        Numero = digits;
    }

    public override string ToString() => Numero;
}
