namespace LicenciamentoSoftware.Client.Models.Dashboard;

public sealed record DashboardResumoResult(
    int TotalClientesFinaisAtivos,
    int TotalAplicacoesAtivas,
    int TotalLicencasAtivas,
    int TotalLicencasInativas,
    LicencasPorTipoResult LicencasPorTipo,
    int LicencasExpirandoEm7Dias,
    int SessoesAtivasAgora,
    int TokensExpirandoEm7Dias,
    int NovasLicencasUltimos30Dias,
    int NovosClientesFinaisUltimos30Dias);

public sealed record LicencasPorTipoResult(
    int Permanente,
    int PorPeriodo,
    int PorUsuarios,
    int PorInstalacao);
