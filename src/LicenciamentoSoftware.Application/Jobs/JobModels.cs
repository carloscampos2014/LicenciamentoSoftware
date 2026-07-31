namespace LicenciamentoSoftware.Application.Jobs;

/// <summary>Informações de licença Por Período para os jobs de expiração e renovação.</summary>
public sealed record LicencaPeriodoJobInfo(
    Guid IdLicenca,
    Guid IdCliente,
    string NomeAplicacao,
    DateTime DataInicio,
    DateTime DataFim,
    bool RenovacaoAutomatica);

/// <summary>Informações de token de licença para os jobs de rotação e notificação.</summary>
public sealed record LicencaTokenJobInfo(
    Guid IdToken,
    Guid IdLicenca,
    Guid IdCliente,
    string NomeAplicacao,
    int ExpiracaoMinutos,
    DateTime CriadoEm,
    bool Ativo);

/// <summary>E-mail e nome do administrador responsável por um cliente.</summary>
public sealed record AdminClienteInfo(
    Guid IdCliente,
    string Email,
    string Nome);
