namespace LicenciamentoSoftware.Application.Dashboard.Results;

/// <summary>
/// Alertas operacionais do tenant — itens que requerem atenção do administrador.
/// </summary>
public sealed record DashboardAlertasResult(
    IReadOnlyList<SessaoInativaAlerta> SessoesInativas,
    IReadOnlyList<InstalacaoAdormentaAlerta> InstalacoesAdormecidas,
    IReadOnlyList<LicencaLimiteAlerta> LicencasNoLimite,
    ErrosValidacaoAlerta ErrosValidacao);

/// <summary>Sessão ativa sem heartbeat por tempo excessivo.</summary>
public sealed record SessaoInativaAlerta(
    Guid IdLicenca,
    Guid IdSessao,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorUsuario,
    DateTime DataUltimaAtividade,
    int HorasInativa);

/// <summary>Instalação registrada sem validação há mais de 30 dias.</summary>
public sealed record InstalacaoAdormentaAlerta(
    Guid IdLicenca,
    Guid IdInstalacao,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string IdentificadorMaquina,
    DateTime? DataUltimaValidacao,
    int DiasAdormecida);

/// <summary>Licença com uso no limite de capacidade (usuários ou instalações).</summary>
public sealed record LicencaLimiteAlerta(
    Guid IdLicenca,
    string ClienteFinalRazaoSocial,
    string AplicativoTitulo,
    string TipoLicenca,
    int UsoAtual,
    int Maximo);

/// <summary>Métricas de erros de validação nas últimas 24h.</summary>
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
