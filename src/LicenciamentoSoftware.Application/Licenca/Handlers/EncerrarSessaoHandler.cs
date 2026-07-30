using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class EncerrarSessaoHandler
{
    private readonly ILicencaSessaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public EncerrarSessaoHandler(ILicencaSessaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<EncerrarSessaoResult> HandleAsync(
        Guid idSessao, CancellationToken ct = default)
    {
        var sessao = await _repo.BuscarPorIdAsync(idSessao, ct);
        if (sessao is null)
            return new EncerrarSessaoResult.NaoEncontrado();

        if (!sessao.Ativo)
            return new EncerrarSessaoResult.JaEncerrada();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.EncerrarAsync(idSessao, ct);
        await _uow.CommitAsync(ct);

        return new EncerrarSessaoResult.Sucesso();
    }
}

public abstract record EncerrarSessaoResult
{
    private EncerrarSessaoResult() { }
    public sealed record Sucesso : EncerrarSessaoResult;
    public sealed record NaoEncontrado : EncerrarSessaoResult;
    public sealed record JaEncerrada : EncerrarSessaoResult;
}
