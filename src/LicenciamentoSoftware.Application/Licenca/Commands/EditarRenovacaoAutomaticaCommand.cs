namespace LicenciamentoSoftware.Application.Licenca.Commands;

public sealed record EditarRenovacaoAutomaticaCommand(
    Guid IdLicenca,
    bool RenovacaoAutomatica);
