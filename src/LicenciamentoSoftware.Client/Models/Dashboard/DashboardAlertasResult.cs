namespace LicenciamentoSoftware.Client.Models.Dashboard;

public sealed record DashboardAlertasResult(
    IReadOnlyList<SessaoInativaAlerta> SessoesInativas,
    IReadOnlyList<InstalacaoAdormentaAlerta> InstalacoesAdormecidas,
    IReadOnlyList<LicencaLimiteAlerta> LicencasNoLimite,
    ErrosValidacaoAlerta ErrosValidacao);

public sealed record SessaoInativaAlerta(
    Guid IdLicenca,
    Guid IdSessao,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorUsuario,
    DateTime DataUltimaAtividade,
    double HorasInativa);

public sealed record InstalacaoAdormentaAlerta(
    Guid IdLicenca,
    Guid IdInstalacao,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorMaquina,
    DateTime? DataUltimaValidacao,
    double DiasAdormecida);

public sealed record LicencaLimiteAlerta(
    Guid IdLicenca,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string TipoLicenca,
    long UsoAtual,
    long Maximo);

public sealed record ErrosValidacaoAlerta(
    long TotalErros,
    IReadOnlyList<ErrosPorMotivo> PorMotivo,
    IReadOnlyList<LicencaComMaisErros> LicencasComMaisErros);

public sealed record ErrosPorMotivo(string Motivo, long Total);

public sealed record LicencaComMaisErros(
    Guid IdLicenca,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    long TotalErros);
