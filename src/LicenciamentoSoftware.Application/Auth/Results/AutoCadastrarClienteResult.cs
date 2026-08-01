namespace LicenciamentoSoftware.Application.Auth.Results;

public abstract record AutoCadastrarClienteResult
{
    private AutoCadastrarClienteResult() { }

    public sealed record Sucesso(Guid IdCliente, Guid IdUsuario) : AutoCadastrarClienteResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : AutoCadastrarClienteResult;
    public sealed record InscricaoJaExiste : AutoCadastrarClienteResult;
    public sealed record EmailJaEmUso : AutoCadastrarClienteResult;
}
