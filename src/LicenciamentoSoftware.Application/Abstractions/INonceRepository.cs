namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta de persistência para nonces anti-replay.
/// Garante que cada nonce seja processado no máximo uma vez dentro da janela de tempo.
/// </summary>
public interface INonceRepository
{
    /// <summary>Verifica se o nonce já foi registrado (replay detectado).</summary>
    Task<bool> ExisteAsync(string nonce, CancellationToken cancellationToken = default);

    /// <summary>Registra o nonce com sua data de expiração.</summary>
    Task RegistrarAsync(string nonce, DateTime expiraEm,
        CancellationToken cancellationToken = default);
}
