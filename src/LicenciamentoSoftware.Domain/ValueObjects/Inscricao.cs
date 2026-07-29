using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace LicenciamentoSoftware.Domain.ValueObjects;

/// <summary>
/// Value object que encapsula CPF (PessoaFisica) ou CNPJ (PessoaJuridica).
/// Armazena apenas dígitos — sem formatação.
/// </summary>
public sealed record Inscricao
{
    private static readonly Regex SomenteDigitos = new(@"^\d+$", RegexOptions.Compiled);

    public TipoInscricao Tipo { get; }
    public string Numero { get; }

    public Inscricao(TipoInscricao tipo, string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("Número de inscrição é obrigatório.");

        var apenasDigitos = Regex.Replace(numero, @"\D", "");

        if (tipo == TipoInscricao.PessoaFisica && !ValidarCpf(apenasDigitos))
            throw new DomainException("CPF inválido.");

        if (tipo == TipoInscricao.PessoaJuridica && !ValidarCnpj(apenasDigitos))
            throw new DomainException("CNPJ inválido.");

        Tipo = tipo;
        Numero = apenasDigitos;
    }

    // Validação de CPF por dígito verificador
    private static bool ValidarCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;

        var soma = 0;
        for (var i = 0; i < 9; i++) soma += (cpf[i] - '0') * (10 - i);
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;
        if (d1 != (cpf[9] - '0')) return false;

        soma = 0;
        for (var i = 0; i < 10; i++) soma += (cpf[i] - '0') * (11 - i);
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;
        return d2 == (cpf[10] - '0');
    }

    // Validação de CNPJ por dígito verificador
    private static bool ValidarCnpj(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;

        int[] m1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] m2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var soma = 0;
        for (var i = 0; i < 12; i++) soma += (cnpj[i] - '0') * m1[i];
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;
        if (d1 != (cnpj[12] - '0')) return false;

        soma = 0;
        for (var i = 0; i < 13; i++) soma += (cnpj[i] - '0') * m2[i];
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;
        return d2 == (cnpj[13] - '0');
    }

    public override string ToString() => Numero;
}
