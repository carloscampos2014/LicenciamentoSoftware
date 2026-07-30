using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;

namespace LicenciamentoSoftware.Application.Cliente.Handlers;

public sealed class DesativarClienteHandler
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesativarClienteHandler(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DesativarClienteResult> HandleAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var existente = await _repo.BuscarPorIdAsync(id, ct);
        if (existente is null)
            return new DesativarClienteResult.NaoEncontrado();

        if (!existente.Ativo)
            return new DesativarClienteResult.JaInativo();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.DesativarAsync(id, ct);
        await _uow.CommitAsync(ct);

        return new DesativarClienteResult.Sucesso();
    }
}

public abstract record DesativarClienteResult
{
    private DesativarClienteResult() { }
    public sealed record Sucesso : DesativarClienteResult;
    public sealed record NaoEncontrado : DesativarClienteResult;
    public sealed record JaInativo : DesativarClienteResult;
}
