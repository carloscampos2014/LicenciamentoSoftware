namespace LicenciamentoSoftware.Application.Licenca.Results;

/// <summary>
/// Resultado discriminado da operação de validação de instalação.
/// </summary>
public abstract record ValidarInstalacaoResult
{
    private ValidarInstalacaoResult() { }

    /// <summary>
    /// Instalação autorizada.
    /// <para>
    /// <see cref="JaRegistrada"/> indica se a máquina já estava registrada (idempotência).
    /// </para>
    /// </summary>
    public sealed record Sucesso(Guid IdInstalacao, bool JaRegistrada) : ValidarInstalacaoResult;

    /// <summary>Input inválido (validação FluentValidation).</summary>
    public sealed record Invalido(IReadOnlyList<string> Erros) : ValidarInstalacaoResult;

    /// <summary>Licença não encontrada para o ID informado.</summary>
    public sealed record LicencaNaoEncontrada : ValidarInstalacaoResult;

    /// <summary>Licença existe mas está inativa (desativada manualmente).</summary>
    public sealed record LicencaInativa : ValidarInstalacaoResult;

    /// <summary>Licença Por Período com data de expiração ultrapassada.</summary>
    public sealed record LicencaExpirada : ValidarInstalacaoResult;

    /// <summary>
    /// Limite de instalações registradas atingido para esta licença.
    /// Retorna a quantidade máxima configurada.
    /// </summary>
    public sealed record LimiteInstalacoesAtingido(int QuantidadeMaxima) : ValidarInstalacaoResult;

    /// <summary>
    /// Tipo de licença não é Por Instalação.
    /// O cliente deve usar o endpoint de validação de login.
    /// </summary>
    public sealed record TipoLicencaIncompativel(string Motivo) : ValidarInstalacaoResult;
}
