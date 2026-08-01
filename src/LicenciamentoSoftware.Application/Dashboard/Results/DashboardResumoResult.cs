namespace LicenciamentoSoftware.Application.Dashboard.Results;

/// <summary>
/// Métricas gerais do tenant para o dashboard — visão consolidada em uma única query.
/// </summary>
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

/// <summary>
/// Breakdown de licenças ativas por tipo de licença.
/// </summary>
public sealed record LicencasPorTipoResult(
    int Permanente,
    int PorPeriodo,
    int PorUsuarios,
    int PorInstalacao);
