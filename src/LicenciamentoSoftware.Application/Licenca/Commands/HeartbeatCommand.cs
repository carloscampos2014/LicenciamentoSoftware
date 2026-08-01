namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Atualiza a data de última atividade de uma sessão ativa (keep-alive).
/// Usado exclusivamente por licenças do tipo Por Usuários.
/// </summary>
public sealed record HeartbeatCommand(
    Guid IdLicenca,
    Guid IdSessao,
    string? IpOrigem = null);
