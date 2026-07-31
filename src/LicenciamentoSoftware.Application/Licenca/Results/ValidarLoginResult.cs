namespace LicenciamentoSoftware.Application.Licenca.Results;

/// <summary>
/// Resultado discriminado da operação de validação de login.
/// </summary>
public abstract record ValidarLoginResult
{
    private ValidarLoginResult() { }

    /// <summary>
    /// Acesso autorizado.
    /// <para>
    /// <see cref="IdSessao"/> é preenchido apenas para licenças Por Usuários.
    /// Para Permanente e Por Período, a sessão não é gerenciada aqui.
    /// </para>
    /// </summary>
    public sealed record Sucesso(Guid? IdSessao) : ValidarLoginResult;

    /// <summary>Input inválido (validação FluentValidation).</summary>
    public sealed record Invalido(IReadOnlyList<string> Erros) : ValidarLoginResult;

    /// <summary>Licença não encontrada para o ID informado.</summary>
    public sealed record LicencaNaoEncontrada : ValidarLoginResult;

    /// <summary>Licença existe mas está inativa (desativada manualmente).</summary>
    public sealed record LicencaInativa : ValidarLoginResult;

    /// <summary>Licença Por Período com data de expiração ultrapassada.</summary>
    public sealed record LicencaExpirada : ValidarLoginResult;

    /// <summary>
    /// Limite global de usuários simultâneos atingido.
    /// Retorna a quantidade máxima configurada para a licença.
    /// </summary>
    public sealed record LimiteUsuariosAtingido(int QuantidadeMaxima) : ValidarLoginResult;

    /// <summary>
    /// Limite de sessões por usuário atingido para o identificador informado.
    /// Retorna o máximo permitido por usuário.
    /// </summary>
    public sealed record LimiteSessionsPorUsuarioAtingido(int MaxSessoesPorUsuario) : ValidarLoginResult;

    /// <summary>
    /// Tipo de licença não suporta validação de login (ex: Por Instalação).
    /// O cliente deve usar o endpoint de validação de instalação.
    /// </summary>
    public sealed record TipoLicencaIncompativel(string Motivo) : ValidarLoginResult;
}
