using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.Cliente.Validators;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Application.Cliente.Handlers;

public sealed class AtualizarClienteHandler
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly AtualizarClienteValidator _validator;

    public AtualizarClienteHandler(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new AtualizarClienteValidator();
    }

    public async Task<AtualizarClienteResult> HandleAsync(
        AtualizarClienteCommand command,
        CancellationToken ct = default)
    {
        // 1. Validação de entrada
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new AtualizarClienteResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Verifica existência
        var existente = await _repo.BuscarPorIdAsync(command.Id, ct);
        if (existente is null)
            return new AtualizarClienteResult.NaoEncontrado();

        // 3. Valida value objects antes de persistir
        try
        {
            _ = new Email(command.Email);
            if (command.Telefone is not null) _ = new Telefone(command.Telefone);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new AtualizarClienteResult.Invalido([ex.Message]);
        }

        // 4. Persiste — Dapper recebe campos diretamente (sem entidade completa)
        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarAsync(command, ct);
        await _uow.CommitAsync(ct);

        var atualizado = await _repo.BuscarPorIdAsync(command.Id, ct);
        return new AtualizarClienteResult.Sucesso(atualizado!);
    }
}

public abstract record AtualizarClienteResult
{
    private AtualizarClienteResult() { }
    public sealed record Sucesso(ClienteResult Cliente) : AtualizarClienteResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : AtualizarClienteResult;
    public sealed record NaoEncontrado : AtualizarClienteResult;
}
