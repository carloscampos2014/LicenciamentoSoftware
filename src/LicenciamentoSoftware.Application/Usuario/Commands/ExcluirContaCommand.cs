namespace LicenciamentoSoftware.Application.Usuario.Commands;

/// <summary>
/// LGPD Art. 18 — solicitação de exclusão/anonimização de dados pessoais.
/// Requer confirmação da senha atual para autenticar a intenção.
/// </summary>
public sealed record ExcluirContaCommand(
    Guid IdUsuario,
    Guid IdCliente,
    string SenhaAtual);
