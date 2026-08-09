namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta de saída para persistência de solicitações de reset de 2FA.
/// </summary>
public interface ISolicitacaoReset2FARepository
{
    /// <summary>Salva o token de confirmação (hash SHA-256) com expiração de 15 minutos.</summary>
    Task SalvarTokenAsync(Guid idUsuario, string tokenHash, DateTime expiraEm,
        string? ipOrigem, CancellationToken ct = default);

    /// <summary>Busca o token de confirmação pelo hash. Retorna null se não encontrado.</summary>
    Task<TokenConfirmacaoReset?> BuscarTokenAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Marca o token como usado e cria a solicitação com status Pendente.</summary>
    Task<Guid> ConfirmarECriarSolicitacaoAsync(Guid idToken, CancellationToken ct = default);

    /// <summary>Lista solicitações pendentes para o Painel Admin.</summary>
    Task<IReadOnlyList<SolicitacaoReset2FAInfo>> ListarPendentesAsync(CancellationToken ct = default);

    /// <summary>Busca uma solicitação pelo ID.</summary>
    Task<SolicitacaoReset2FAInfo?> BuscarPorIdAsync(Guid idSolicitacao, CancellationToken ct = default);

    /// <summary>Aprova a solicitação (Admin) — marca como Aprovado e define processado_em.</summary>
    Task AprovarAsync(Guid idSolicitacao, CancellationToken ct = default);

    /// <summary>Rejeita a solicitação (Admin) — marca como Rejeitado e define processado_em.</summary>
    Task RejeitarAsync(Guid idSolicitacao, CancellationToken ct = default);
}

public sealed record TokenConfirmacaoReset(
    Guid Id, Guid IdUsuario, DateTime ExpiraEm, string? IpOrigem);

public sealed record SolicitacaoReset2FAInfo(
    Guid Id, Guid IdUsuario, string NomeUsuario, string EmailUsuario,
    string NomeCliente, string Status, string? IpOrigem, DateTime CriadoEm);
