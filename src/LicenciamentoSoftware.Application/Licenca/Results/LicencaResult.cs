namespace LicenciamentoSoftware.Application.Licenca.Results;

/// <summary>DTO de saída para licença com detalhe embutido.</summary>
public sealed record LicencaResult(
    Guid Id,
    Guid IdCliente,
    Guid IdClienteFinal,
    string ClienteFinalRazaoSocial,
    Guid IdAplicativo,
    string AplicativoTitulo,
    Guid IdTipoLicenca,
    string TipoLicencaDescricao,
    DateTime DataCadastro,
    bool Ativo,
    DetalhePeriodoResult? Periodo,
    DetalheUsuariosResult? Usuarios,
    DetalheInstalacaoResult? Instalacao,
    IReadOnlyList<SessaoResult>? Sessoes,
    IReadOnlyList<InstalacaoRegistradaResult>? InstalacoesRegistradas);

public sealed record DetalhePeriodoResult(
    DateTime DataInicio,
    DateTime DataFim,
    bool RenovacaoAutomatica);

public sealed record DetalheUsuariosResult(
    int QuantidadeMaxima,
    int MaxSessoesPorUsuario,
    int TempoLimiteSessaoHoras);

public sealed record DetalheInstalacaoResult(
    int QuantidadeMaxima);

public sealed record SessaoResult(
    Guid Id,
    Guid LicencaId,
    string IdentificadorUsuario,
    DateTime DataLogin,
    DateTime DataUltimaAtividade,
    bool Ativo);

public sealed record InstalacaoRegistradaResult(
    Guid Id,
    Guid LicencaId,
    string IdentificadorMaquina,
    DateTime DataRegistro,
    bool Ativo);
