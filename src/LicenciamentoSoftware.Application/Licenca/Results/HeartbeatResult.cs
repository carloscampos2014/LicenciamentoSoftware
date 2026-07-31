namespace LicenciamentoSoftware.Application.Licenca.Results;

/// <summary>
/// Resultado discriminado da operação de heartbeat (keep-alive de sessão).
/// </summary>
public abstract record HeartbeatResult
{
    private HeartbeatResult() { }

    /// <summary>Atividade registrada com sucesso.</summary>
    public sealed record Sucesso : HeartbeatResult;

    /// <summary>Sessão não encontrada para o ID informado.</summary>
    public sealed record SessaoNaoEncontrada : HeartbeatResult;

    /// <summary>Sessão encontrada mas já está encerrada.</summary>
    public sealed record SessaoEncerrada : HeartbeatResult;

    /// <summary>
    /// Sessão pertence a licença diferente da informada no comando.
    /// Previne enumeração entre tenants.
    /// </summary>
    public sealed record AcessoNegado : HeartbeatResult;
}
