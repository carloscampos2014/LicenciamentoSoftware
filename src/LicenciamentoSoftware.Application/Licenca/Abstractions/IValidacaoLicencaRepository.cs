namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

// UUIDs dos tipos de licença (seed V001)
// Duplicados aqui para evitar acoplamento com a camada de Application de gestão
// Os mesmos valores estão em EmitirLicencaHandler e na migration V001.
public static class TiposLicencaIds
{
    public static readonly Guid Permanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Periodo    = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Usuarios   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Instalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");
}

/// <summary>
/// Informações enriquecidas de uma licença para o fluxo de validação.
/// Agrega licença + tipo + detalhe em uma única query.
/// </summary>
public sealed record LicencaValidacaoInfo(
    Guid Id,
    Guid IdCliente,
    bool Ativo,
    Guid IdTipoLicenca,
    // Por Período
    DateTime? DataFim,
    bool? RenovacaoAutomatica,
    // Por Usuários
    int? QuantidadeMaximaUsuarios,
    int? MaxSessoesPorUsuario,
    int? TempoLimiteSessaoHoras,
    // Por Instalação
    int? QuantidadeMaximaInstalacoes);

/// <summary>
/// Porta de leitura enriquecida para o fluxo de validação de licença.
/// Consolida licença + tipo + detalhe em uma única roundtrip ao banco.
/// </summary>
public interface IValidacaoLicencaRepository
{
    /// <summary>
    /// Busca a licença com todas as informações de detalhe necessárias para validação.
    /// Retorna <c>null</c> se a licença não existir.
    /// </summary>
    Task<LicencaValidacaoInfo?> BuscarParaValidacaoAsync(
        Guid idLicenca,
        CancellationToken ct = default);
}
