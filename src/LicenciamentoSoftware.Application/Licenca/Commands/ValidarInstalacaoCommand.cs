namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Valida e registra uma instalação de software em uma máquina.
/// Aplicável exclusivamente a licenças do tipo Por Instalação.
/// A operação é idempotente: máquina já registrada retorna sucesso com JaRegistrada=true.
/// </summary>
public sealed record ValidarInstalacaoCommand(
    Guid IdLicenca,
    string IdentificadorMaquina,
    string? IpOrigem = null);
