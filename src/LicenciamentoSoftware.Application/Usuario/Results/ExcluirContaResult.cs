namespace LicenciamentoSoftware.Application.Usuario.Results;

public abstract record ExcluirContaResult
{
    /// <summary>Dados anonimizados com sucesso. Sessão deve ser encerrada.</summary>
    public sealed record Sucesso : ExcluirContaResult;

    /// <summary>Senha incorreta.</summary>
    public sealed record SenhaInvalida : ExcluirContaResult;

    /// <summary>Usuário não encontrado.</summary>
    public sealed record NaoEncontrado : ExcluirContaResult;

    /// <summary>
    /// Último AdministradorCliente — não pode ser excluído enquanto for o único admin ativo.
    /// </summary>
    public sealed record UltimoAdministrador : ExcluirContaResult;
}
