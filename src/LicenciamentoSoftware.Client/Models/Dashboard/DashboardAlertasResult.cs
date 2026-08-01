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
    int HorasInativa);

public sealed record InstalacaoAdormentaAlerta(
    Guid IdLicenca,
    Guid IdInstalacao,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorMaquina,
    DateTime? DataUltimaValidacao,
    int DiasAdormecida);

public sealed record LicencaLimiteAlerta(
    Guid IdLicenca,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string TipoLicenca,
    int UsoAtual,
    int Maximo);

public sealed record ErrosValidacaoAlerta(
    int TotalErros,
    IReadOnlyList<ErrosPorMotivo> PorMotivo,
    IReadOnlyList<LicencaComMaisErros> LicencasComMaisErros);

public sealed record ErrosPorMotivo(string Motivo, int Total);

public sealed record LicencaComMaisErros(
    Guid IdLicenca,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    int TotalErros);
