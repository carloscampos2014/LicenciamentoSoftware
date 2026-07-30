using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class LiberarInstalacaoHandler
{
    private readonly ILicencaInstalacaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public LiberarInstalacaoHandler(ILicencaInstalacaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<LiberarInstalacaoResult> HandleAsync(
        Guid idInstalacao, CancellationToken ct = default)
    {
        var instalacao = await _repo.BuscarPorIdAsync(idInstalacao, ct);
        if (instalacao is null)
            return new LiberarInstalacaoResult.NaoEncontrado();

        if (!instalacao.Ativo)
            return new LiberarInstalacaoResult.JaLiberada();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.LiberarAsync(idInstalacao, ct);
        await _uow.CommitAsync(ct);

        return new LiberarInstalacaoResult.Sucesso();
    }
}

public abstract record LiberarInstalacaoResult
{
    private LiberarInstalacaoResult() { }
    public sealed record Sucesso : LiberarInstalacaoResult;
    public sealed record NaoEncontrado : LiberarInstalacaoResult;
    public sealed record JaLiberada : LiberarInstalacaoResult;
}
