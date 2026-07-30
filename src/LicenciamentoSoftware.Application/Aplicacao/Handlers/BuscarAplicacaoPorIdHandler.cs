using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Results;

namespace LicenciamentoSoftware.Application.Aplicacao.Handlers;

public sealed class BuscarAplicacaoPorIdHandler
{
    private readonly IAplicacaoRepository _repo;

    public BuscarAplicacaoPorIdHandler(IAplicacaoRepository repo) => _repo = repo;

    public async Task<AplicacaoResult?> HandleAsync(Guid id, CancellationToken ct = default)
        => await _repo.BuscarPorIdAsync(id, ct);
}
