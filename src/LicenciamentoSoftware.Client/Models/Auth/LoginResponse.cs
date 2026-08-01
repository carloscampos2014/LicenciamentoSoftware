namespace LicenciamentoSoftware.Client.Models.Auth;

public sealed record LoginResponse(
    string? AccessToken,
    string? RefreshToken,
    DateTime? Expiracao,
    string? Nome,
    string? Papel,
    bool Requer2FA = false,
    string? TokenTemporario = null);
