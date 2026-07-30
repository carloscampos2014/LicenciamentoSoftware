using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;

namespace LicenciamentoSoftware.Application.Aplicacao.Handlers;

public sealed class DesativarAplicacaoHandler
{
    private readonly IAplicacaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesativarAplicacaoHandler(IAplicacaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DesativarAplicacaoResult> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var existente = await _repo.BuscarPorIdAsync(id, ct);
        if (existente is null)
            return new DesativarAplicacaoResult.NaoEncontrado();

        if (!existente.Ativo)
            return new DesativarAplicacaoResult.JaInativo();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.DesativarAsync(id, ct);
        await _uow.CommitAsync(ct);

        return new DesativarAplicacaoResult.Sucesso();
    }
}

public abstract record DesativarAplicacaoResult
{
    private DesativarAplicacaoResult() { }
    public sealed record Sucesso : DesativarAplicacaoResult;
    public sealed record NaoEncontrado : DesativarAplicacaoResult;
    public sealed record JaInativo : DesativarAplicacaoResult;
}
