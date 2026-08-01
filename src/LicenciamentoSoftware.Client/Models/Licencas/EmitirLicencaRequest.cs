namespace LicenciamentoSoftware.Client.Models.Licencas;

public sealed record EmitirLicencaRequest(
    Guid IdClienteFinal,
    Guid IdAplicativo,
    DetalhePeriodoRequest? Periodo,
    DetalheUsuariosRequest? Usuarios,
    DetalheInstalacaoRequest? Instalacao,
    bool EmitirToken = false,
    int? ExpiracaoTokenMinutos = null);

public sealed record DetalhePeriodoRequest(
    DateTime DataInicio,
    DateTime DataFim,
    bool RenovacaoAutomatica = false);

public sealed record DetalheUsuariosRequest(
    int QuantidadeMaxima,
    int MaxSessoesPorUsuario = 5,
    int TempoLimiteSessaoHoras = 24);

public sealed record DetalheInstalacaoRequest(int QuantidadeMaxima);
