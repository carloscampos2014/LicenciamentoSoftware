using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace LicenciamentoSoftware.Domain.ValueObjects;

/// <summary>
/// Value object que encapsula CPF (PessoaFisica) ou CNPJ (PessoaJuridica).
///
/// CNPJ alfanumérico: a IN RFB 2.229/2024 permite letras nas 8 primeiras posições
/// (raiz + ordem). Os 2 dígitos verificadores continuam numéricos.
/// Armazena sem formatação (pontos, barras, hífens removidos), letras em maiúsculas.
/// </summary>
public sealed record Inscricao
{
    public TipoInscricao Tipo { get; }
    public string Numero { get; }

    public Inscricao(TipoInscricao tipo, string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("Número de inscrição é obrigatório.");

        if (tipo == TipoInscricao.PessoaFisica)
        {
            // CPF: somente dígitos, remove formatação
            var apenasDigitos = Regex.Replace(numero, @"\D", "");
            if (!ValidarCpf(apenasDigitos))
                throw new DomainException("CPF inválido.");
            Tipo = tipo;
            Numero = apenasDigitos;
        }
        else
        {
            // CNPJ: remove formatação (pontos, barra, hífen) mas preserva letras maiúsculas
            var normalizado = Regex.Replace(numero.ToUpperInvariant(), @"[.\-/]", "");
            if (!ValidarCnpj(normalizado))
                throw new DomainException("CNPJ inválido.");
            Tipo = tipo;
            Numero = normalizado;
        }
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

    /// <summary>
    /// Validação de CNPJ com suporte a alfanumérico (IN RFB 2.229/2024).
    /// Cada caractere é convertido para valor numérico:
    ///   '0'-'9' → valor direto
    ///   'A'-'Z' → valor - 'A' + 10  (A=10, B=11, ..., Z=35)
    /// Os dois últimos caracteres são sempre dígitos verificadores numéricos.
    /// </summary>
    private static bool ValidarCnpj(string cnpj)
    {
        if (cnpj.Length != 14) return false;

        // Verificar se todos os caracteres são válidos (dígito ou letra maiúscula)
        foreach (var c in cnpj)
            if (!char.IsDigit(c) && !(c >= 'A' && c <= 'Z'))
                return false;

        // Rejeitar CNPJs com todos caracteres iguais (ex: 00000000000000)
        if (cnpj.Distinct().Count() == 1) return false;

        // Os 2 últimos devem ser dígitos (verificadores)
        if (!char.IsDigit(cnpj[12]) || !char.IsDigit(cnpj[13])) return false;

        int[] m1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] m2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var soma = 0;
        for (var i = 0; i < 12; i++) soma += ValorChar(cnpj[i]) * m1[i];
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;
        if (d1 != (cnpj[12] - '0')) return false;

        soma = 0;
        for (var i = 0; i < 13; i++) soma += ValorChar(cnpj[i]) * m2[i];
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;
        return d2 == (cnpj[13] - '0');
    }

    /// <summary>
    /// Converte um caractere para seu valor numérico no algoritmo CNPJ alfanumérico.
    /// Dígitos: valor direto. Letras A-Z: A=10, B=11, ..., Z=35.
    /// </summary>
    private static int ValorChar(char c)
        => char.IsDigit(c) ? c - '0' : c - 'A' + 10;

    public override string ToString() => Numero;
}
