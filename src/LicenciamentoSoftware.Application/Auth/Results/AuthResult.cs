namespace LicenciamentoSoftware.Application.Auth.Results;

/// <summary>
/// Resultado discriminado de operações de autenticação.
/// A Application nunca retorna tipos HTTP — retorna resultados explícitos.
/// </summary>
public abstract record AuthResult
{
    /// <summary>Login bem-sucedido sem 2FA — JWT emitido.</summary>
    public sealed record Sucesso(
        string AccessToken,
        string RefreshToken,
        DateTime Expiracao,
        string Nome,
        string Papel) : AuthResult;

    /// <summary>Login bem-sucedido mas 2FA obrigatório — aguarda código TOTP.</summary>
    public sealed record Requer2FA(string TokenTemporario) : AuthResult;

    /// <summary>Credenciais inválidas ou usuário inativo.</summary>
    public sealed record Negado(string Motivo) : AuthResult;

    /// <summary>
    /// Email válido mas conta sem senha (anonimização LGPD).
    /// O portal deve oferecer criação de nova senha.
    /// Contém um token temporário com o ID do usuário para autorizar a definição de senha.
    /// </summary>
    public sealed record SemSenha(string TokenTemporario) : AuthResult;

    /// <summary>Token TOTP inválido ou expirado.</summary>
    public sealed record TotpInvalido(string Motivo) : AuthResult;

    /// <summary>Refresh token inválido, expirado ou revogado.</summary>
    public sealed record TokenInvalido(string Motivo) : AuthResult;
}

/// <summary>Resultado de configuração de TOTP.</summary>
public sealed record ConfigurarTotpResult(string Segredo, string QrCodeUri);

/// <summary>Resultado de registro de usuário.</summary>
public abstract record RegistrarResult
{
    public sealed record Sucesso(Guid IdUsuario, string Nome, string Papel) : RegistrarResult;
    public sealed record ClienteNaoEncontrado : RegistrarResult;
    public sealed record EmailJaEmUso : RegistrarResult;
}
