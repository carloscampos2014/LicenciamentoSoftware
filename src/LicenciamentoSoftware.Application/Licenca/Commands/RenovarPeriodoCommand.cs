namespace LicenciamentoSoftware.Application.Licenca.Commands;

public sealed record RenovarPeriodoCommand(
    Guid IdLicenca,
    DateTime NovaDataFim);
