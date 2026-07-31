namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Encerra explicitamente uma sessão de validação.
/// A operação é idempotente: sessão já encerrada resulta em sucesso.
/// </summary>
public sealed record LogoutValidacaoCommand(
    Guid IdLicenca,
    Guid IdSessao);
