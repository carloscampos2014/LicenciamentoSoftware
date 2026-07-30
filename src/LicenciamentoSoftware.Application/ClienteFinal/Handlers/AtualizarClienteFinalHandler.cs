using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Commands;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using LicenciamentoSoftware.Application.ClienteFinal.Validators;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Application.ClienteFinal.Handlers;

public sealed class AtualizarClienteFinalHandler
{
    private readonly IClienteFinalRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly AtualizarClienteFinalValidator _validator;

    public AtualizarClienteFinalHandler(IClienteFinalRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new AtualizarClienteFinalValidator();
    }

    public async Task<AtualizarClienteFinalResult> HandleAsync(
        AtualizarClienteFinalCommand command, CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new AtualizarClienteFinalResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var existente = await _repo.BuscarPorIdAsync(command.Id, ct);
        if (existente is null)
            return new AtualizarClienteFinalResult.NaoEncontrado();

        try
        {
            _ = new Email(command.Email);
            if (command.Telefone is not null) _ = new Telefone(command.Telefone);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new AtualizarClienteFinalResult.Invalido([ex.Message]);
        }

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarAsync(command, ct);
        await _uow.CommitAsync(ct);

        var atualizado = await _repo.BuscarPorIdAsync(command.Id, ct);
        return new AtualizarClienteFinalResult.Sucesso(atualizado!);
    }
}

public abstract record AtualizarClienteFinalResult
{
    private AtualizarClienteFinalResult() { }
    public sealed record Sucesso(ClienteFinalResult ClienteFinal) : AtualizarClienteFinalResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : AtualizarClienteFinalResult;
    public sealed record NaoEncontrado : AtualizarClienteFinalResult;
}
