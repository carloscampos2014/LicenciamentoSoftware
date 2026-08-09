namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta de saída para persistência de tokens de recuperação de senha.
/// </summary>
public interface IRecuperacaoSenhaRepository
{
    /// <summary>Salva um novo token (como hash SHA-256) com expiração de 1 hora.</summary>
    Task SalvarAsync(Guid idUsuario, string tokenHash, DateTime expiraEm, CancellationToken ct = default);

    /// <summary>
    /// Busca um token válido (não expirado, não usado) pelo hash.
    /// Retorna null se não encontrado ou inválido.
    /// </summary>
    Task<TokenRecuperacao?> BuscarPorHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Marca o token como usado (invalidação após uso).</summary>
    Task MarcarComoUsadoAsync(Guid idToken, CancellationToken ct = default);
}

public sealed record TokenRecuperacao(Guid Id, Guid IdUsuario, DateTime ExpiraEm);
