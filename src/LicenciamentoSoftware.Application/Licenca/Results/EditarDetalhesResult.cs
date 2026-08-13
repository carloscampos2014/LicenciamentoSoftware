namespace LicenciamentoSoftware.Application.Licenca.Results;

public abstract record EditarDetalhesResult
{
    private EditarDetalhesResult() { }
    public sealed record Sucesso : EditarDetalhesResult;
    public sealed record LicencaNaoEncontrada : EditarDetalhesResult;
    public sealed record LicencaInativa : EditarDetalhesResult;
    public sealed record TipoIncompativel(string Motivo) : EditarDetalhesResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : EditarDetalhesResult;
}
