using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using LicenciamentoSoftware.Application.Aplicacao.Validators;

namespace LicenciamentoSoftware.Application.Aplicacao.Handlers;

public sealed class CriarAplicacaoHandler
{
    private readonly IAplicacaoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly CriarAplicacaoValidator _validator;

    public CriarAplicacaoHandler(IAplicacaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new CriarAplicacaoValidator();
    }

    public async Task<CriarAplicacaoResult> HandleAsync(
        CriarAplicacaoCommand command, CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new CriarAplicacaoResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var tipoExiste = await _repo.ExisteTipoLicencaAsync(command.IdTipoLicenca, ct);
        if (!tipoExiste)
            return new CriarAplicacaoResult.TipoLicencaNaoEncontrado();

        Domain.Entities.Aplicacao aplicacao;
        try
        {
            aplicacao = Domain.Entities.Aplicacao.Criar(
                command.IdCliente, command.Titulo, command.IdTipoLicenca, command.Descricao);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new CriarAplicacaoResult.Invalido([ex.Message]);
        }

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.InserirAsync(aplicacao, ct);
        await _uow.CommitAsync(ct);

        return new CriarAplicacaoResult.Sucesso(new AplicacaoResult(
            aplicacao.Id, aplicacao.IdCliente, aplicacao.Titulo,
            aplicacao.Descricao, aplicacao.IdTipoLicenca, aplicacao.Ativo));
    }
}

public abstract record CriarAplicacaoResult
{
    private CriarAplicacaoResult() { }
    public sealed record Sucesso(AplicacaoResult Aplicacao) : CriarAplicacaoResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : CriarAplicacaoResult;
    public sealed record TipoLicencaNaoEncontrado : CriarAplicacaoResult;
}
