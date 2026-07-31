namespace LicenciamentoSoftware.Application.Licenca.Results;

/// <summary>
/// Resultado discriminado da operação de logout via endpoint de validação.
/// A operação é idempotente: sessão já encerrada também resulta em <see cref="Sucesso"/>.
/// </summary>
public abstract record LogoutValidacaoResult
{
    private LogoutValidacaoResult() { }

    /// <summary>Sessão encerrada (ou já estava encerrada — idempotente).</summary>
    public sealed record Sucesso : LogoutValidacaoResult;

    /// <summary>Sessão não encontrada para o ID informado.</summary>
    public sealed record SessaoNaoEncontrada : LogoutValidacaoResult;

    /// <summary>
    /// Sessão pertence a licença diferente da informada no comando.
    /// Previne enumeração entre tenants.
    /// </summary>
    public sealed record AcessoNegado : LogoutValidacaoResult;
}
