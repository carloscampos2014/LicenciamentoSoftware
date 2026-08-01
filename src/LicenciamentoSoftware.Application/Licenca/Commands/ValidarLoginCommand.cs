namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Valida o acesso de um usuário a uma licença.
/// Aplicável a todos os tipos: Permanente, Por Período, Por Usuários.
/// </summary>
public sealed record ValidarLoginCommand(
    Guid IdLicenca,
    string IdentificadorUsuario,
    string? IpOrigem = null);
