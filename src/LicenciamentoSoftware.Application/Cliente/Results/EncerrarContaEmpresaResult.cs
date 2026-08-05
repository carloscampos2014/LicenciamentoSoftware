namespace LicenciamentoSoftware.Application.Cliente.Results;

public abstract record EncerrarContaEmpresaResult
{
    private EncerrarContaEmpresaResult() { }

    /// <summary>Conta encerrada com sucesso.</summary>
    public sealed record Sucesso : EncerrarContaEmpresaResult;

    /// <summary>Empresa não encontrada ou já inativa.</summary>
    public sealed record NaoEncontrado : EncerrarContaEmpresaResult;

    /// <summary>Senha informada está incorreta.</summary>
    public sealed record SenhaInvalida : EncerrarContaEmpresaResult;

    /// <summary>Empresa já foi encerrada anteriormente.</summary>
    public sealed record JaEncerrada : EncerrarContaEmpresaResult;
}
