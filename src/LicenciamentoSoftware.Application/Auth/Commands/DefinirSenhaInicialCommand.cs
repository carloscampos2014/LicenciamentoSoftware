namespace LicenciamentoSoftware.Application.Auth.Commands;

/// <summary>
/// Define uma nova senha para uma conta anonimizada (sem senha).
/// Requer o token temporário emitido pelo LoginHandler quando detecta SemSenha.
/// </summary>
public sealed record DefinirSenhaInicialCommand(
    string TokenTemporario,
    string NovaSenha);
