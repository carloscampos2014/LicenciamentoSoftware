using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class DesativarLicencaHandler
{
    private readonly ILicencaGestaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesativarLicencaHandler(ILicencaGestaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<DesativarLicencaResult> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var existente = await _repo.BuscarPorIdAsync(id, ct);
        if (existente is null)
            return new DesativarLicencaResult.NaoEncontrado();

        if (!existente.Ativo)
            return new DesativarLicencaResult.JaInativo();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.DesativarAsync(id, ct);
        await _uow.CommitAsync(ct);

        return new DesativarLicencaResult.Sucesso();
    }
}

public abstract record DesativarLicencaResult
{
    private DesativarLicencaResult() { }
    public sealed record Sucesso : DesativarLicencaResult;
    public sealed record NaoEncontrado : DesativarLicencaResult;
    public sealed record JaInativo : DesativarLicencaResult;
}
