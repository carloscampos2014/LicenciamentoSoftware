using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Abstractions;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

public sealed class DesativarUsuarioHandler
{
    private readonly IUsuarioGestaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesativarUsuarioHandler(IUsuarioGestaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DesativarUsuarioResult> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var existente = await _repo.BuscarPorIdAsync(id, ct);
        if (existente is null)
            return new DesativarUsuarioResult.NaoEncontrado();

        if (!existente.Ativo)
            return new DesativarUsuarioResult.JaInativo();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.DesativarAsync(id, ct);
        await _uow.CommitAsync(ct);

        return new DesativarUsuarioResult.Sucesso();
    }
}

public abstract record DesativarUsuarioResult
{
    private DesativarUsuarioResult() { }
    public sealed record Sucesso : DesativarUsuarioResult;
    public sealed record NaoEncontrado : DesativarUsuarioResult;
    public sealed record JaInativo : DesativarUsuarioResult;
}
