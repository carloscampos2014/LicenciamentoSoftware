using System.Text.RegularExpressions;

namespace LicenciamentoSoftware.Application.Common;

/// <summary>
/// Regras de validação de senha forte — reutilizadas em handlers e validators.
/// Critérios: mínimo 8 caracteres, 1 maiúscula, 1 número, 1 caractere especial.
/// </summary>
public static class PasswordRules
{
    public const string MensagemErro =
        "A senha deve ter no mínimo 8 caracteres, uma letra maiúscula, um número e um caractere especial (!@#$%^&*).";

    private static readonly Regex TemMaiuscula   = new(@"[A-Z]",           RegexOptions.Compiled);
    private static readonly Regex TemNumero      = new(@"[0-9]",           RegexOptions.Compiled);
    private static readonly Regex TemEspecial    = new(@"[!@#$%^&*]",      RegexOptions.Compiled);

    /// <summary>Retorna true se a senha atende a todos os critérios de força.</summary>
    public static bool IsSenhaForte(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            return false;

        return TemMaiuscula.IsMatch(senha)
            && TemNumero.IsMatch(senha)
            && TemEspecial.IsMatch(senha);
    }

    /// <summary>
    /// Calcula a força da senha de 0 a 4.
    /// 0 = vazia, 1 = só comprimento, 2 = + maiúscula, 3 = + número, 4 = + especial.
    /// </summary>
    public static int ForcaNivel(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha)) return 0;

        int nivel = 0;
        if (senha.Length >= 8)          nivel++;
        if (TemMaiuscula.IsMatch(senha)) nivel++;
        if (TemNumero.IsMatch(senha))    nivel++;
        if (TemEspecial.IsMatch(senha))  nivel++;

        return nivel;
    }
}
