namespace LicenciamentoSoftware.Application.Licenca.Abstractions;

/// <summary>
/// Motivos de erro possíveis na validação de licença.
/// Usados para categorização nas métricas do dashboard.
/// </summary>
public static class MotivoErroValidacao
{
    public const string TokenInvalido       = "token_invalido";
    public const string LicencaInativa      = "licenca_inativa";
    public const string LicenceNaoEncontrada = "licenca_nao_encontrada";
    public const string LimiteExcedido      = "limite_excedido";
    public const string SessaoInvalida      = "sessao_invalida";
    public const string InstalacaoInvalida  = "instalacao_invalida";
    public const string ReplayDetectado     = "replay_detectado";
    public const string LicencaExpirada     = "licenca_expirada";
}

/// <summary>
/// Tipos de operação de validação registrados no log.
/// </summary>
public static class TipoOperacaoValidacao
{
    public const string Login      = "login";
    public const string Heartbeat  = "heartbeat";
    public const string Logout     = "logout";
    public const string Instalacao = "instalacao";
}

/// <summary>
/// Porta de escrita para o log de validações.
/// Usado para registrar tentativas (sucesso e erro) e alimentar as métricas do dashboard.
/// </summary>
public interface IValidacaoLogRepository
{
    /// <summary>
    /// Registra uma entrada no log de validação.
    /// Fire-and-forget seguro — não deve bloquear o fluxo principal de validação.
    /// </summary>
    Task InserirAsync(
        Guid idLicenca,
        string tipoOperacao,
        string resultado,
        string? motivoErro = null,
        string? ipOrigem = null,
        CancellationToken ct = default);
}
