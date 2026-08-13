namespace LicenciamentoSoftware.Application.Licenca.Commands;

public sealed record EditarDetalhesInstalacaoCommand(
    Guid IdLicenca,
    int QuantidadeMaxima);
