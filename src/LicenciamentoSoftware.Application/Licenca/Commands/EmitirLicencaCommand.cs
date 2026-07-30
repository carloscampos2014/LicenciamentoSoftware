namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Solicita a emissão de uma nova licença.
/// Exatamente um dos blocos de detalhe deve ser informado,
/// compatível com o TipoLicenca da Aplicacao.
/// </summary>
public sealed record EmitirLicencaCommand(
    Guid IdClienteFinal,
    Guid IdAplicativo,
    /// <summary>Detalhe para licença do tipo Por Período.</summary>
    DetalhePeriodoCommand? Periodo,
    /// <summary>Detalhe para licença do tipo Por Usuários.</summary>
    DetalheUsuariosCommand? Usuarios,
    /// <summary>Detalhe para licença do tipo Por Instalação.</summary>
    DetalheInstalacaoCommand? Instalacao,
    /// <summary>Se true, emite o token HMAC junto com a licença.</summary>
    bool EmitirToken = false,
    /// <summary>Sobrescreve o tempo de expiração padrão do token (em minutos).</summary>
    int? ExpiracaoTokenMinutos = null);

public sealed record DetalhePeriodoCommand(
    DateTime DataInicio,
    DateTime DataFim,
    bool RenovacaoAutomatica = false);

public sealed record DetalheUsuariosCommand(
    int QuantidadeMaxima,
    int MaxSessoesPorUsuario = 5,
    int TempoLimiteSessaoHoras = 24);

public sealed record DetalheInstalacaoCommand(
    int QuantidadeMaxima);
