using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using LicenciamentoSoftware.Application.Aplicacao.Validators;

namespace LicenciamentoSoftware.Application.Aplicacao.Handlers;

public sealed class AtualizarAplicacaoHandler
{
    private readonly IAplicacaoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly AtualizarAplicacaoValidator _validator;

    public AtualizarAplicacaoHandler(IAplicacaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new AtualizarAplicacaoValidator();
    }

    public async Task<AtualizarAplicacaoResult> HandleAsync(
        AtualizarAplicacaoCommand command, CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new AtualizarAplicacaoResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var existente = await _repo.BuscarPorIdAsync(command.Id, ct);
        if (existente is null)
            return new AtualizarAplicacaoResult.NaoEncontrado();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarAsync(command, ct);
        await _uow.CommitAsync(ct);

        var atualizado = await _repo.BuscarPorIdAsync(command.Id, ct);
        return new AtualizarAplicacaoResult.Sucesso(atualizado!);
    }
}

public abstract record AtualizarAplicacaoResult
{
    private AtualizarAplicacaoResult() { }
    public sealed record Sucesso(AplicacaoResult Aplicacao) : AtualizarAplicacaoResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : AtualizarAplicacaoResult;
    public sealed record NaoEncontrado : AtualizarAplicacaoResult;
}
