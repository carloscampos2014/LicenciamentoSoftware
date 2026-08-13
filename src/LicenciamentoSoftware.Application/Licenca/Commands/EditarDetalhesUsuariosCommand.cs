namespace LicenciamentoSoftware.Application.Licenca.Commands;

public sealed record EditarDetalhesUsuariosCommand(
    Guid IdLicenca,
    int QuantidadeMaxima,
    int MaxSessoesPorUsuario);
