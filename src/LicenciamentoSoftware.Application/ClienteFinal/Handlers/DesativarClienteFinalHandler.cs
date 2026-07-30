using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;

namespace LicenciamentoSoftware.Application.ClienteFinal.Handlers;

public sealed class DesativarClienteFinalHandler
{
    private readonly IClienteFinalRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesativarClienteFinalHandler(IClienteFinalRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DesativarClienteFinalResult> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var existente = await _repo.BuscarPorIdAsync(id, ct);
        if (existente is null)
            return new DesativarClienteFinalResult.NaoEncontrado();

        if (!existente.Ativo)
            return new DesativarClienteFinalResult.JaInativo();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.DesativarAsync(id, ct);
        await _uow.CommitAsync(ct);

        return new DesativarClienteFinalResult.Sucesso();
    }
}

public abstract record DesativarClienteFinalResult
{
    private DesativarClienteFinalResult() { }
    public sealed record Sucesso : DesativarClienteFinalResult;
    public sealed record NaoEncontrado : DesativarClienteFinalResult;
    public sealed record JaInativo : DesativarClienteFinalResult;
}
